param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$TargetAbi,

    [Parameter(Mandatory = $true)]
    [string]$SourceUrl,

    [Parameter(Mandatory = $true)]
    [string]$Checksum,

    [Parameter(Mandatory = $false)]
    [string]$Timestamp = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),

    [Parameter(Mandatory = $false)]
    [string]$Changelog = "Initial release."
)

$ErrorActionPreference = "Stop"

$manifestPath = Join-Path $PSScriptRoot "..\manifest.json"
$pluginGuid = "ad3000ca-4bcb-4b4d-a67f-b9a80cd81892"

if (Test-Path $manifestPath) {
    $manifest = Get-Content -Raw -Path $manifestPath | ConvertFrom-Json
} else {
    $manifest = @()
}

$entries = @($manifest)
$entry = $entries | Where-Object { $_.guid -eq $pluginGuid } | Select-Object -First 1

if ($null -eq $entry) {
    $entry = [pscustomobject]@{
        guid = $pluginGuid
        name = "AudD Music Recognition"
        description = "Recognize music playing inside Jellyfin videos by sending a short audio clip to the AudD API."
        overview = "Recognize music in the Jellyfin player"
        owner = "custom"
        category = "Music"
        versions = @()
    }
    $entries += $entry
}

$versionEntry = [pscustomobject]@{
    version = $Version
    changelog = $Changelog
    targetAbi = $TargetAbi
    sourceUrl = $SourceUrl
    checksum = $Checksum.ToLowerInvariant()
    timestamp = $Timestamp
}

$remainingVersions = @($entry.versions) | Where-Object { $_.version -ne $Version }
$entry.versions = @($versionEntry) + $remainingVersions

$entries | ConvertTo-Json -Depth 10 | Set-Content -Path $manifestPath -Encoding UTF8

Write-Host "Updated manifest for version $Version"
