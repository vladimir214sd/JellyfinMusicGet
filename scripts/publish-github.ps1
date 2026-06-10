param(
    [Parameter(Mandatory = $false)]
    [string]$RepositoryName = "JellyfinMusicGet",

    [Parameter(Mandatory = $false)]
    [string]$Version = "1.0.0.0",

    [Parameter(Mandatory = $false)]
    [string]$TargetAbi = "10.11.0.0",

    [Parameter(Mandatory = $false)]
    [string]$Changelog = "Initial release."
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$tokenPath = Join-Path $root "token.txt"

if (-not (Test-Path $tokenPath)) {
    throw "token.txt was not found."
}

function Get-GitHubToken {
    $token = (Get-Content -Raw -Path $tokenPath).Trim()
    if ([string]::IsNullOrWhiteSpace($token)) {
        throw "token.txt is empty."
    }

    return $token
}

function New-GitHubHeaders {
    $token = Get-GitHubToken

    return @{
        "Accept" = "application/vnd.github+json"
        "Authorization" = "Bearer $token"
        "X-GitHub-Api-Version" = "2022-11-28"
        "User-Agent" = "JellyfinMusicGetPublisher"
    }
}

function Get-GitHubErrorMessage {
    param([object]$ErrorRecord)

    $response = $ErrorRecord.Exception.Response
    if ($null -eq $response) {
        return $ErrorRecord.Exception.Message
    }

    try {
        $stream = $response.GetResponseStream()
        if ($null -eq $stream) {
            return $ErrorRecord.Exception.Message
        }

        $reader = [System.IO.StreamReader]::new($stream)
        return $reader.ReadToEnd()
    } catch {
        return $ErrorRecord.Exception.Message
    }
}

function Invoke-GitHub {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Method,

        [Parameter(Mandatory = $true)]
        [string]$Uri,

        [Parameter(Mandatory = $false)]
        [object]$Body,

        [Parameter(Mandatory = $false)]
        [switch]$IgnoreNotFound
    )

    $maxAttempts = 4

    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        try {
            $headers = New-GitHubHeaders

            if ($null -ne $Body) {
                $json = $Body | ConvertTo-Json -Depth 20
                return Invoke-RestMethod -Method $Method -Uri $Uri -Headers $headers -Body $json -ContentType "application/json"
            }

            return Invoke-RestMethod -Method $Method -Uri $Uri -Headers $headers
        } catch {
            $statusCode = $null
            if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
                $statusCode = [int]$_.Exception.Response.StatusCode
            }

            if ($IgnoreNotFound -and $statusCode -eq 404) {
                return $null
            }

            $message = Get-GitHubErrorMessage -ErrorRecord $_
            $isRateLimit = $statusCode -eq 429 -or
                ($statusCode -eq 403 -and $message -match '(?i)(rate limit|secondary rate limit|abuse)')
            $isTransient = $statusCode -eq 401 -or
                $isRateLimit -or
                $statusCode -eq 500 -or
                $statusCode -eq 502 -or
                $statusCode -eq 503 -or
                $statusCode -eq 504

            if ($isTransient -and $attempt -lt $maxAttempts) {
                $delaySeconds = [Math]::Pow(2, $attempt)
                Write-Warning "GitHub API returned HTTP $statusCode for $Method $Uri. Retrying in $delaySeconds seconds ($attempt/$maxAttempts)."
                Start-Sleep -Seconds $delaySeconds
                continue
            }

            if ($statusCode -eq 401) {
                throw "GitHub authentication failed after $maxAttempts attempts. Replace token.txt with a valid token and grant repository Contents: Read and write permission. Last response: $message"
            }

            throw "GitHub API $Method $Uri failed ($statusCode): $message"
        }
    }
}

function ConvertTo-GitHubPath {
    param([string]$Path)

    return [Uri]::EscapeDataString($Path).Replace("%2F", "/")
}

function Get-RelativePath {
    param([string]$FullPath)

    $rootPath = ([System.IO.Path]::GetFullPath($root)).TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    $rootUri = [Uri]::new($rootPath)
    $fileUri = [Uri]::new([System.IO.Path]::GetFullPath($FullPath))
    return [Uri]::UnescapeDataString($rootUri.MakeRelativeUri($fileUri).ToString()).Replace("/", "\")
}

function Should-PublishFile {
    param([System.IO.FileInfo]$File)

    $relative = (Get-RelativePath -FullPath $File.FullName).Replace("\", "/")
    if ($relative -eq "token.txt") {
        return $false
    }

    if ($relative -eq "manifest.json") {
        return $false
    }

    if ($relative -match '(^|/)(bin|obj|node_modules|TestResults|coverage|\.git|\.vs|\.idea)(/|$)') {
        return $false
    }

    if ($relative.EndsWith(".user", [StringComparison]::OrdinalIgnoreCase) -or
        $relative.EndsWith(".suo", [StringComparison]::OrdinalIgnoreCase) -or
        $relative.EndsWith(".nupkg", [StringComparison]::OrdinalIgnoreCase) -or
        $relative.EndsWith(".snupkg", [StringComparison]::OrdinalIgnoreCase) -or
        $relative.EndsWith(".log", [StringComparison]::OrdinalIgnoreCase)) {
        return $false
    }

    return $true
}

$user = Invoke-GitHub -Method "GET" -Uri "https://api.github.com/user"
$owner = $user.login
$repo = Invoke-GitHub -Method "GET" -Uri "https://api.github.com/repos/$owner/$RepositoryName" -IgnoreNotFound

if ($null -eq $repo) {
    Write-Host "Creating repository $owner/$RepositoryName"
    $repo = Invoke-GitHub -Method "POST" -Uri "https://api.github.com/user/repos" -Body @{
        name = $RepositoryName
        description = "Jellyfin plugin for recognizing music with the AudD API."
        private = $false
        auto_init = $false
        has_issues = $true
        has_projects = $false
        has_wiki = $false
    }
} else {
    Write-Host "Using existing repository $owner/$RepositoryName"
}

$files = Get-ChildItem -Path $root -Recurse -File -Force |
    Where-Object { Should-PublishFile -File $_ } |
    Sort-Object @{ Expression = { ((Get-RelativePath -FullPath $_.FullName).Replace("\", "/") -like ".github/workflows/*") } }, FullName

foreach ($file in $files) {
    $relativePath = (Get-RelativePath -FullPath $file.FullName).Replace("\", "/")
    $encodedPath = ConvertTo-GitHubPath -Path $relativePath
    $existing = Invoke-GitHub -Method "GET" -Uri "https://api.github.com/repos/$owner/$RepositoryName/contents/$encodedPath" -IgnoreNotFound
    $content = [Convert]::ToBase64String([System.IO.File]::ReadAllBytes($file.FullName))

    if ($null -ne $existing -and $existing.content) {
        $remoteContent = ([string]$existing.content) -replace "\s", ""
        if ($remoteContent -eq $content) {
            Write-Host "Unchanged $relativePath"
            continue
        }
    }

    $body = @{
        message = "Publish $relativePath"
        content = $content
    }

    if ($null -ne $existing -and $existing.sha) {
        $body.sha = $existing.sha
    }

    Invoke-GitHub -Method "PUT" -Uri "https://api.github.com/repos/$owner/$RepositoryName/contents/$encodedPath" -Body $body | Out-Null
    Write-Host "Uploaded $relativePath"
    Start-Sleep -Milliseconds 250
}

$repo = Invoke-GitHub -Method "GET" -Uri "https://api.github.com/repos/$owner/$RepositoryName"
$branch = if ([string]::IsNullOrWhiteSpace($repo.default_branch)) { "main" } else { $repo.default_branch }

$workflow = $null
for ($attempt = 1; $attempt -le 5 -and $null -eq $workflow; $attempt++) {
    Start-Sleep -Seconds 3
    $workflow = Invoke-GitHub -Method "GET" -Uri "https://api.github.com/repos/$owner/$RepositoryName/actions/workflows/release.yml" -IgnoreNotFound
}

$workflowDispatched = $false
if ($null -ne $workflow) {
    Invoke-GitHub -Method "POST" -Uri "https://api.github.com/repos/$owner/$RepositoryName/actions/workflows/release.yml/dispatches" -Body @{
        ref = $branch
        inputs = @{
            version = $Version
            targetAbi = $TargetAbi
            changelog = $Changelog
        }
    } | Out-Null
    $workflowDispatched = $true
    Write-Host "Dispatched release workflow for $Version"
} else {
    Write-Host "Release workflow was uploaded, but GitHub did not expose it yet. Run it manually from Actions if it does not start."
}

[pscustomobject]@{
    Repository = "https://github.com/$owner/$RepositoryName"
    Manifest = "https://raw.githubusercontent.com/$owner/$RepositoryName/$branch/manifest.json"
    WorkflowDispatched = $workflowDispatched
}
