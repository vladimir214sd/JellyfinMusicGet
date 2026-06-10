(function () {
    'use strict';

    var TICKS_PER_SECOND = 10000000;
    var PLUGIN_NAME = 'AuddMusicRecognitionPlugin';
    var PLUGIN_ID = 'ad3000ca-4bcb-4b4d-a67f-b9a80cd81892';
    var DEFAULT_REQUEST_TIMEOUT_MS = 45000;
    window.__auddMusicRecognitionOverlayLoaded = true;

    function firstFunctionResult(candidates) {
        for (var i = 0; i < candidates.length; i += 1) {
            try {
                var value = candidates[i]();
                if (value !== undefined && value !== null) {
                    return value;
                }
            } catch (_) {
            }
        }

        return null;
    }

    function pickFirstValue(values) {
        for (var i = 0; i < values.length; i += 1) {
            if (values[i] !== undefined && values[i] !== null) {
                return values[i];
            }
        }

        return null;
    }

    function asArray(value) {
        if (!value) {
            return [];
        }

        return Array.prototype.slice.call(value);
    }

    function normalizeGuid(value) {
        if (!value || typeof value !== 'string') {
            return null;
        }

        var trimmed = value.trim();
        if (/^[0-9a-f]{32}$/i.test(trimmed)) {
            return [
                trimmed.slice(0, 8),
                trimmed.slice(8, 12),
                trimmed.slice(12, 16),
                trimmed.slice(16, 20),
                trimmed.slice(20)
            ].join('-');
        }

        return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(trimmed) ? trimmed : null;
    }

    function parseNumber(value) {
        var number = Number(value);
        return Number.isFinite(number) ? number : null;
    }

    function positiveNumber(value) {
        var number = parseNumber(value);
        return number && number > 0 ? number : null;
    }

    function readResponseValue(response, names) {
        if (!response) {
            return null;
        }

        for (var i = 0; i < names.length; i += 1) {
            if (response[names[i]] !== undefined && response[names[i]] !== null) {
                return response[names[i]];
            }
        }

        return null;
    }

    function getResponseStatus(response) {
        var status = readResponseValue(response, ['status', 'Status']);
        return status ? String(status).toLowerCase() : '';
    }

    function normalizeTicks(value, videoSeconds) {
        var number = Number(value);

        if (!Number.isFinite(number) || number < 0) {
            return 0;
        }

        var currentVideoSeconds = Number(videoSeconds);
        if (Number.isFinite(currentVideoSeconds) && currentVideoSeconds >= 0) {
            if (Math.abs(number - currentVideoSeconds) < 2) {
                return Math.round(number * TICKS_PER_SECOND);
            }

            if (Math.abs((number / 1000) - currentVideoSeconds) < 2) {
                return Math.round(number * 10000);
            }

            if (Math.abs((number / TICKS_PER_SECOND) - currentVideoSeconds) < 2) {
                return Math.round(number);
            }
        }

        if (number > TICKS_PER_SECOND) {
            return Math.round(number);
        }

        if (number > 10000) {
            return Math.round(number * 10000);
        }

        return Math.round(number * TICKS_PER_SECOND);
    }

    function withTimeout(promiseFactory, timeoutMs) {
        var controller = typeof AbortController !== 'undefined' ? new AbortController() : null;
        var timeoutError = new Error('Recognition timed out');
        timeoutError.isTimeout = true;

        return new Promise(function (resolve, reject) {
            var settled = false;
            var timeoutId = window.setTimeout(function () {
                if (settled) {
                    return;
                }

                settled = true;

                if (controller) {
                    controller.abort();
                }

                reject(timeoutError);
            }, timeoutMs);

            var promise;
            try {
                promise = promiseFactory(controller ? controller.signal : undefined);
            } catch (error) {
                settled = true;
                window.clearTimeout(timeoutId);
                reject(error);
                return;
            }

            promise.then(function (result) {
                if (settled) {
                    return;
                }

                settled = true;
                window.clearTimeout(timeoutId);
                resolve(result);
            }, function (error) {
                if (settled) {
                    return;
                }

                settled = true;
                window.clearTimeout(timeoutId);
                reject(error);
            });
        });
    }

    function jsonFetch(url, payload, timeoutMs) {
        var headers = {
            'Content-Type': 'application/json'
        };

        var token = firstFunctionResult([
            function () { return window.ApiClient && typeof window.ApiClient.accessToken === 'function' ? window.ApiClient.accessToken() : null; },
            function () { return window.ApiClient && typeof window.ApiClient.serverInfo === 'function' ? window.ApiClient.serverInfo().AccessToken : null; },
            function () { return window.ApiClient && window.ApiClient._serverInfo ? window.ApiClient._serverInfo.AccessToken : null; }
        ]);

        if (token) {
            headers.Authorization = 'MediaBrowser Token="' + token + '"';
            headers['X-Emby-Token'] = token;
        }

        var requestUrl = firstFunctionResult([
            function () { return window.ApiClient && typeof window.ApiClient.getUrl === 'function' ? window.ApiClient.getUrl(url) : null; },
            function () { return '/' + url.replace(/^\/+/, ''); }
        ]);

        return withTimeout(function (signal) {
            return fetch(requestUrl, {
                method: 'POST',
                headers: headers,
                body: JSON.stringify(payload),
                credentials: 'same-origin',
                signal: signal
            }).then(function (response) {
                if (!response.ok) {
                    var responseError = new Error('HTTP ' + response.status);
                    responseError.httpStatus = response.status;
                    throw responseError;
                }

                return response.json();
            });
        }, timeoutMs || DEFAULT_REQUEST_TIMEOUT_MS).catch(function (fetchError) {
            if (fetchError && (fetchError.isTimeout || fetchError.name === 'AbortError')) {
                throw fetchError.isTimeout ? fetchError : new Error('Recognition timed out');
            }

            if (fetchError && fetchError.httpStatus && fetchError.httpStatus !== 401 && fetchError.httpStatus !== 403) {
                throw fetchError;
            }

            if (window.ApiClient && typeof window.ApiClient.ajax === 'function' && typeof window.ApiClient.getUrl === 'function') {
                return withTimeout(function () {
                    return window.ApiClient.ajax({
                        type: 'POST',
                        url: window.ApiClient.getUrl(url),
                        contentType: 'application/json',
                        dataType: 'json',
                        data: JSON.stringify(payload)
                    });
                }, timeoutMs || DEFAULT_REQUEST_TIMEOUT_MS);
            }

            throw fetchError;
        });
    }

    function getText(response) {
        if (!response) {
            return 'No match';
        }

        var status = getResponseStatus(response);
        if (status === 'recognized') {
            return [
                readResponseValue(response, ['artist', 'Artist']),
                readResponseValue(response, ['title', 'Title'])
            ].filter(Boolean).join(' - ') || 'Recognized';
        }

        return readResponseValue(response, ['statusMessage', 'StatusMessage'])
            || (status === 'no_match' ? 'No match' : 'Recognition failed');
    }

    function shouldCacheResponse(response) {
        var status = getResponseStatus(response);
        return status === 'recognized';
    }

    function formatBytes(bytes) {
        var value = Number(bytes);
        if (!Number.isFinite(value) || value < 0) {
            return '';
        }

        if (value < 1024) {
            return Math.round(value) + ' B';
        }

        if (value < 1024 * 1024) {
            return (value / 1024).toFixed(1) + ' KB';
        }

        return (value / 1024 / 1024).toFixed(2) + ' MB';
    }

    function formatClipDebug(response) {
        var durationTicks = readResponseValue(response, ['clipDurationTicks', 'ClipDurationTicks']);
        var sizeBytes = readResponseValue(response, ['clipSizeBytes', 'ClipSizeBytes']);
        var parts = [];

        if (durationTicks !== undefined && durationTicks !== null) {
            parts.push((Number(durationTicks) / TICKS_PER_SECOND).toFixed(1) + ' s');
        }

        var sizeText = formatBytes(sizeBytes);
        if (sizeText) {
            parts.push(sizeText);
        }

        return parts.length ? 'Отправлено: ' + parts.join(', ') : '';
    }

    function getActiveVideo() {
        var videos = document.querySelectorAll('video');
        for (var i = 0; i < videos.length; i += 1) {
            if (videos[i].getClientRects().length > 0) {
                return videos[i];
            }
        }

        return videos.length ? videos[0] : null;
    }

    function collectPlaybackUrls(video) {
        var urls = [];

        if (video) {
            urls.push(video.currentSrc, video.src);
            asArray(video.querySelectorAll('source')).forEach(function (source) {
                urls.push(source.src);
            });
        }

        asArray(document.querySelectorAll('video source, audio source')).forEach(function (source) {
            urls.push(source.src);
        });

        try {
            asArray(window.performance && typeof window.performance.getEntriesByType === 'function'
                ? window.performance.getEntriesByType('resource')
                : []).forEach(function (entry) {
                if (entry && entry.name) {
                    urls.push(entry.name);
                }
            });
        } catch (_) {
        }

        urls.push(window.location.href);

        return urls.filter(function (value, index, array) {
            return value && array.indexOf(value) === index;
        }).reverse();
    }

    function parsePlaybackUrl(url) {
        if (!url || typeof url !== 'string') {
            return {};
        }

        var result = {};

        try {
            var parsed = new URL(url, window.location.href);
            var itemId = normalizeGuid(
                parsed.searchParams.get('ItemId')
                || parsed.searchParams.get('itemId')
                || parsed.searchParams.get('Id')
                || parsed.searchParams.get('id'));

            if (!itemId) {
                var pathMatch = parsed.pathname.match(/\/(?:Videos|Items|Audio)\/([0-9a-f-]{32,36})(?:\/|$)/i);
                itemId = pathMatch ? normalizeGuid(pathMatch[1]) : null;
            }

            if (itemId) {
                result.itemId = itemId;
            }

            result.mediaSourceId = parsed.searchParams.get('MediaSourceId') || parsed.searchParams.get('mediaSourceId') || null;
            result.audioStreamIndex = parseNumber(parsed.searchParams.get('AudioStreamIndex') || parsed.searchParams.get('audioStreamIndex'));
            result.positionTicks = parseNumber(parsed.searchParams.get('StartTimeTicks') || parsed.searchParams.get('startTimeTicks'));
        } catch (_) {
        }

        return result;
    }

    function getPlaybackContextFromUrls(video) {
        var urls = collectPlaybackUrls(video);
        var result = {};

        for (var i = 0; i < urls.length; i += 1) {
            var parsed = parsePlaybackUrl(urls[i]);

            if (!result.itemId && parsed.itemId) {
                result.itemId = parsed.itemId;
            }

            if (!result.mediaSourceId && parsed.mediaSourceId) {
                result.mediaSourceId = parsed.mediaSourceId;
            }

            if (result.audioStreamIndex === undefined && parsed.audioStreamIndex !== undefined && parsed.audioStreamIndex !== null) {
                result.audioStreamIndex = parsed.audioStreamIndex;
            }

            if (!result.positionTicks && parsed.positionTicks && parsed.positionTicks > 0) {
                result.positionTicks = parsed.positionTicks;
            }

            if (result.itemId && result.positionTicks) {
                break;
            }
        }

        return result;
    }

    window[PLUGIN_NAME] = async function () {
        return class AuddMusicRecognitionPlugin {
            constructor(dependencies) {
                if (window.__auddMusicRecognitionOverlayInstance) {
                    return window.__auddMusicRecognitionOverlayInstance;
                }

                this.dependencies = dependencies || {};
                this.cache = new Map();
                this.overlay = null;
                this.button = null;
                this.result = null;
                this.debug = null;
                this.currentRoot = null;
                this.currentMediaIdentity = null;
                this.lastTriggerAt = 0;
                this.requestSequence = 0;
                this.isBusy = false;
                this.settings = {
                    showDebugInfo: false,
                    requestTimeoutMs: DEFAULT_REQUEST_TIMEOUT_MS
                };
                this.ensureOverlay = this.ensureOverlay.bind(this);
                this.handleTrigger = this.handleTrigger.bind(this);
                this.runRecognition = this.runRecognition.bind(this);

                this.injectStyles();
                this.settingsPromise = withTimeout(function () {
                    return this.loadSettings();
                }.bind(this), 3000).catch(function () {
                });
                this.observer = new MutationObserver(this.ensureOverlay);
                this.observer.observe(document.documentElement, { childList: true, subtree: true });
                this.interval = window.setInterval(this.ensureOverlay, 1000);
                this.ensureOverlay();
                window.__auddMusicRecognitionOverlayInstance = this;
            }

            destroy() {
                if (this.observer) {
                    this.observer.disconnect();
                }

                if (this.interval) {
                    window.clearInterval(this.interval);
                }

                if (this.overlay) {
                    this.overlay.remove();
                }
            }

            injectStyles() {
                if (document.getElementById('auddMusicRecognitionStyles')) {
                    return;
                }

                var style = document.createElement('style');
                style.id = 'auddMusicRecognitionStyles';
                style.textContent = [
                    '.auddRecognitionOverlay{position:fixed;top:calc(env(safe-area-inset-top,0px) + 16px);right:calc(env(safe-area-inset-right,0px) + 16px);z-index:99999;display:flex;align-items:center;gap:8px;max-width:min(420px,calc(100vw - 32px));pointer-events:auto;font-family:inherit;}',
                    '.auddRecognitionButton{width:40px;height:40px;border:0;border-radius:50%;display:inline-flex;align-items:center;justify-content:center;background:rgba(20,20,20,.72);color:#fff;box-shadow:0 4px 18px rgba(0,0,0,.28);cursor:pointer;touch-action:manipulation;}',
                    '.auddRecognitionButton:disabled{opacity:.62;cursor:default;}',
                    '.auddRecognitionButton svg{width:21px;height:21px;fill:currentColor;}',
                    '.auddRecognitionResult{min-height:32px;max-width:360px;padding:7px 10px;border-radius:7px;background:rgba(20,20,20,.72);color:#fff;font-size:13px;line-height:1.25;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;box-shadow:0 4px 18px rgba(0,0,0,.28);}',
                    '.auddRecognitionResult:empty{display:none;}',
                    '.auddRecognitionDebug{min-height:24px;max-width:260px;padding:5px 8px;border-radius:7px;background:rgba(20,20,20,.55);color:rgba(255,255,255,.82);font-size:11px;line-height:1.2;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;}',
                    '.auddRecognitionDebug:empty{display:none;}',
                    '@media (max-width: 640px){.auddRecognitionOverlay{top:12px;right:12px;max-width:calc(100vw - 24px);}.auddRecognitionResult{max-width:calc(100vw - 76px);font-size:12px;}}'
                ].join('');
                document.head.appendChild(style);
            }

            loadSettings() {
                if (!window.ApiClient || typeof window.ApiClient.getPluginConfiguration !== 'function') {
                    return Promise.resolve();
                }

                return window.ApiClient.getPluginConfiguration(PLUGIN_ID).then(function (config) {
                    this.settings.showDebugInfo = config.ShowOverlayDebugInfo === true || config.showOverlayDebugInfo === true;
                }.bind(this)).catch(function () {
                });
            }

            ensureOverlay() {
                var playerRoot = this.findPlayerRoot();

                if (!playerRoot) {
                    if (this.overlay) {
                        this.overlay.remove();
                        this.overlay = null;
                    }

                    this.currentRoot = null;
                    return;
                }

                var root = document.body;
                if (this.overlay && this.currentRoot === root && root.contains(this.overlay)) {
                    this.syncPlaybackIdentity();
                    return;
                }

                if (this.overlay) {
                    this.overlay.remove();
                }

                this.currentRoot = root;

                this.overlay = document.createElement('div');
                this.overlay.className = 'auddRecognitionOverlay';

                this.button = document.createElement('button');
                this.button.type = 'button';
                this.button.className = 'auddRecognitionButton';
                this.button.title = 'Recognize music';
                this.button.setAttribute('aria-label', 'Recognize music');
                this.button.innerHTML = '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 3v10.55A4 4 0 1 0 14 17V7h4V3h-6Z"/></svg>';
                this.button.addEventListener('click', this.handleTrigger, true);
                this.button.addEventListener('pointerup', this.handleTrigger, true);

                this.result = document.createElement('div');
                this.result.className = 'auddRecognitionResult';
                this.result.setAttribute('aria-live', 'polite');

                this.debug = document.createElement('div');
                this.debug.className = 'auddRecognitionDebug';

                this.overlay.appendChild(this.button);
                this.overlay.appendChild(this.result);
                this.overlay.appendChild(this.debug);
                root.appendChild(this.overlay);

                this.setBusy(this.isBusy);
                this.syncPlaybackIdentity();
            }

            findPlayerRoot() {
                var video = getActiveVideo();
                if (!video) {
                    return null;
                }

                return video.closest('.videoOsdPage,.htmlvideoplayer,.videoPlayerContainer,.mediaPlayer,.playerContainer,.nowPlayingPage')
                    || video.parentElement
                    || document.body;
            }

            getPlaybackContext() {
                var playbackManager = this.dependencies.playbackManager || window.playbackManager || window.PlaybackManager;
                var player = firstFunctionResult([
                    function () { return playbackManager && playbackManager.getCurrentPlayer ? playbackManager.getCurrentPlayer() : null; },
                    function () { return playbackManager && playbackManager.currentPlayer ? playbackManager.currentPlayer() : null; }
                ]);

                var playerInfo = firstFunctionResult([
                    function () { return playbackManager && playbackManager.getPlayerInfo ? playbackManager.getPlayerInfo() : null; },
                    function () { return playbackManager && playbackManager.getPlayerState ? playbackManager.getPlayerState() : null; }
                ]) || {};

                var statePlayState = playerInfo.PlayState || playerInfo.playState || {};
                var stateItem = playerInfo.NowPlayingItem || playerInfo.nowPlayingItem;
                var item = firstFunctionResult([
                    function () { return playbackManager && playbackManager.currentItem ? playbackManager.currentItem(player) : null; },
                    function () { return playbackManager && playbackManager.currentItem ? playbackManager.currentItem() : null; },
                    function () { return playbackManager && playbackManager.getCurrentItem ? playbackManager.getCurrentItem() : null; },
                    function () { return playerInfo.item || playerInfo.Item || playerInfo.currentItem || playerInfo.CurrentItem || stateItem; }
                ]) || {};

                var video = getActiveVideo();
                var fallback = getPlaybackContextFromUrls(video);
                var currentMediaSource = firstFunctionResult([
                    function () { return playbackManager && playbackManager.currentMediaSource ? playbackManager.currentMediaSource(player) : null; },
                    function () { return playbackManager && playbackManager.currentMediaSource ? playbackManager.currentMediaSource() : null; },
                    function () { return playerInfo.MediaSource || playerInfo.mediaSource; }
                ]);
                var videoPosition = video ? positiveNumber(video.currentTime) : null;
                var fallbackPosition = positiveNumber(fallback.positionTicks);
                var position = firstFunctionResult([
                    function () { return statePlayState.PositionTicks || statePlayState.positionTicks; },
                    function () { return playbackManager && playbackManager.currentTime ? playbackManager.currentTime(player) : null; },
                    function () { return playbackManager && playbackManager.getCurrentTicks ? playbackManager.getCurrentTicks() : null; },
                    function () { return playerInfo.positionTicks || playerInfo.PositionTicks; },
                    function () { return videoPosition; },
                    function () { return fallbackPosition; },
                    function () { return video ? video.currentTime : null; }
                ]);

                var mediaSources = item.MediaSources || item.mediaSources || [];
                var mediaSource = currentMediaSource || (mediaSources.length ? mediaSources[0] : {});

                return {
                    itemId: item.Id || item.id || item.ItemId || item.itemId || fallback.itemId,
                    mediaSourceId: item.MediaSourceId || item.mediaSourceId || playerInfo.mediaSourceId || playerInfo.MediaSourceId || mediaSource.Id || mediaSource.id || fallback.mediaSourceId || null,
                    positionTicks: normalizeTicks(position, video ? video.currentTime : null),
                    audioStreamIndex: pickFirstValue([
                        statePlayState.AudioStreamIndex,
                        statePlayState.audioStreamIndex,
                        playerInfo.audioStreamIndex,
                        playerInfo.AudioStreamIndex,
                        mediaSource.DefaultAudioStreamIndex,
                        mediaSource.defaultAudioStreamIndex,
                        mediaSource.AudioStreamIndex,
                        mediaSource.audioStreamIndex,
                        fallback.audioStreamIndex,
                        mediaSource.DefaultAudioStreamIndex,
                        mediaSource.defaultAudioStreamIndex
                    ])
                };
            }

            cacheKey(context) {
                var roundedSeconds = Math.round((context.positionTicks / TICKS_PER_SECOND) / 10) * 10;
                return context.itemId + ':' + roundedSeconds;
            }

            mediaIdentity(context) {
                if (!context || !context.itemId) {
                    return null;
                }

                return [context.itemId, context.mediaSourceId || ''].join(':');
            }

            syncPlaybackIdentity() {
                var context;
                try {
                    context = this.getPlaybackContext();
                } catch (_) {
                    return;
                }

                var identity = this.mediaIdentity(context);
                if (!identity) {
                    return;
                }

                if (!this.currentMediaIdentity) {
                    this.currentMediaIdentity = identity;
                    return;
                }

                if (this.currentMediaIdentity !== identity) {
                    this.currentMediaIdentity = identity;
                    this.requestSequence += 1;
                    this.setBusy(false);
                    this.setResult('');
                    this.setDebug('');
                }
            }

            setBusy(isBusy) {
                this.isBusy = isBusy;

                if (this.button) {
                    this.button.disabled = isBusy;
                }
            }

            setResult(text) {
                if (this.result) {
                    this.result.textContent = text || '';
                }
            }

            setDebug(text) {
                if (this.debug) {
                    this.debug.textContent = text || '';
                }
            }

            consumeEvent(event) {
                if (!event) {
                    return;
                }

                event.preventDefault();
                event.stopPropagation();

                if (typeof event.stopImmediatePropagation === 'function') {
                    event.stopImmediatePropagation();
                }
            }

            handleTrigger(event) {
                this.consumeEvent(event);

                if (this.isBusy) {
                    this.setResult('Still recognizing...');
                    return;
                }

                var now = Date.now();
                if (now - this.lastTriggerAt < 500) {
                    return;
                }

                this.lastTriggerAt = now;
                this.runRecognition();
            }

            async runRecognition() {
                this.setResult('Starting...');
                this.setDebug('');

                if (this.settingsPromise) {
                    await this.settingsPromise;
                }

                var context;
                try {
                    context = this.getPlaybackContext();
                } catch (error) {
                    this.setResult(error && error.message ? error.message : 'Could not read playback');
                    return;
                }

                if (!context.itemId) {
                    this.setResult('No item id');
                    return;
                }

                var identity = this.mediaIdentity(context);
                this.currentMediaIdentity = identity || this.currentMediaIdentity;

                var key = this.cacheKey(context);
                if (this.cache.has(key)) {
                    this.setResult(getText(this.cache.get(key)));
                    return;
                }

                var requestId = this.requestSequence + 1;
                this.requestSequence = requestId;
                this.setBusy(true);
                this.setResult('Recognizing...');

                try {
                    var response = await jsonFetch('Plugins/AuddMusicRecognition/Recognize', {
                        itemId: context.itemId,
                        mediaSourceId: context.mediaSourceId,
                        positionTicks: context.positionTicks,
                        audioStreamIndex: context.audioStreamIndex
                    }, this.settings.requestTimeoutMs);

                    if (requestId !== this.requestSequence || (identity && identity !== this.currentMediaIdentity)) {
                        return;
                    }

                    if (shouldCacheResponse(response)) {
                        this.cache.set(key, response);
                    }

                    if (this.settings.showDebugInfo) {
                        this.setDebug(formatClipDebug(response));
                    }

                    this.setResult(getText(response));
                } catch (error) {
                    if (requestId !== this.requestSequence || (identity && identity !== this.currentMediaIdentity)) {
                        return;
                    }

                    this.setResult(error && error.message ? error.message : 'Recognition failed');
                } finally {
                    if (requestId === this.requestSequence) {
                        this.setBusy(false);
                    }
                }
            }
        };
    };

    function bootStandalone() {
        if (window.__auddMusicRecognitionOverlayInstance) {
            return;
        }

        window[PLUGIN_NAME]().then(function (PluginClass) {
            return new PluginClass({
                playbackManager: window.playbackManager || window.PlaybackManager,
                ServerConnections: window.ServerConnections
            });
        }).catch(function () {
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', bootStandalone, { once: true });
    } else {
        bootStandalone();
    }
}());
