(function () {
    'use strict';

    var TICKS_PER_SECOND = 10000000;
    var PLUGIN_NAME = 'AuddMusicRecognitionPlugin';

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

    function jsonFetch(url, payload) {
        if (window.ApiClient && typeof window.ApiClient.ajax === 'function' && typeof window.ApiClient.getUrl === 'function') {
            return window.ApiClient.ajax({
                type: 'POST',
                url: window.ApiClient.getUrl(url),
                contentType: 'application/json',
                dataType: 'json',
                data: JSON.stringify(payload)
            });
        }

        var headers = {
            'Content-Type': 'application/json'
        };

        var token = firstFunctionResult([
            function () { return window.ApiClient && typeof window.ApiClient.accessToken === 'function' ? window.ApiClient.accessToken() : null; },
            function () { return window.ApiClient && window.ApiClient._serverInfo ? window.ApiClient._serverInfo.AccessToken : null; }
        ]);

        if (token) {
            headers.Authorization = 'MediaBrowser Token="' + token + '"';
        }

        return fetch('/' + url.replace(/^\/+/, ''), {
            method: 'POST',
            headers: headers,
            body: JSON.stringify(payload)
        }).then(function (response) {
            if (!response.ok) {
                throw new Error('HTTP ' + response.status);
            }

            return response.json();
        });
    }

    function getText(response) {
        if (!response) {
            return 'No match';
        }

        if (response.status === 'recognized') {
            return [response.artist, response.title].filter(Boolean).join(' - ') || 'Recognized';
        }

        return response.statusMessage || (response.status === 'no_match' ? 'No match' : 'Recognition failed');
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
                this.currentRoot = null;
                this.ensureOverlay = this.ensureOverlay.bind(this);
                this.handleClick = this.handleClick.bind(this);

                this.injectStyles();
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
                    '.auddRecognitionOverlay{position:absolute;top:calc(env(safe-area-inset-top,0px) + 16px);right:calc(env(safe-area-inset-right,0px) + 16px);z-index:1200;display:flex;align-items:center;gap:8px;max-width:min(420px,calc(100vw - 32px));pointer-events:auto;font-family:inherit;}',
                    '.auddRecognitionButton{width:40px;height:40px;border:0;border-radius:50%;display:inline-flex;align-items:center;justify-content:center;background:rgba(20,20,20,.72);color:#fff;box-shadow:0 4px 18px rgba(0,0,0,.28);cursor:pointer;}',
                    '.auddRecognitionButton:disabled{opacity:.62;cursor:default;}',
                    '.auddRecognitionButton svg{width:21px;height:21px;fill:currentColor;}',
                    '.auddRecognitionResult{min-height:32px;max-width:360px;padding:7px 10px;border-radius:7px;background:rgba(20,20,20,.72);color:#fff;font-size:13px;line-height:1.25;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;box-shadow:0 4px 18px rgba(0,0,0,.28);}',
                    '.auddRecognitionResult:empty{display:none;}',
                    '@media (max-width: 640px){.auddRecognitionOverlay{top:12px;right:12px;max-width:calc(100vw - 24px);}.auddRecognitionResult{max-width:calc(100vw - 76px);font-size:12px;}}'
                ].join('');
                document.head.appendChild(style);
            }

            ensureOverlay() {
                var root = this.findPlayerRoot();

                if (!root) {
                    if (this.overlay) {
                        this.overlay.remove();
                        this.overlay = null;
                    }

                    this.currentRoot = null;
                    return;
                }

                if (this.overlay && this.currentRoot === root && root.contains(this.overlay)) {
                    return;
                }

                if (this.overlay) {
                    this.overlay.remove();
                }

                this.currentRoot = root;

                var computedStyle = window.getComputedStyle(root);
                if (computedStyle.position === 'static') {
                    root.style.position = 'relative';
                }

                this.overlay = document.createElement('div');
                this.overlay.className = 'auddRecognitionOverlay';

                this.button = document.createElement('button');
                this.button.type = 'button';
                this.button.className = 'auddRecognitionButton';
                this.button.title = 'Recognize music';
                this.button.setAttribute('aria-label', 'Recognize music');
                this.button.innerHTML = '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 3v10.55A4 4 0 1 0 14 17V7h4V3h-6Z"/></svg>';
                this.button.addEventListener('click', this.handleClick);

                this.result = document.createElement('div');
                this.result.className = 'auddRecognitionResult';
                this.result.setAttribute('aria-live', 'polite');

                this.overlay.appendChild(this.button);
                this.overlay.appendChild(this.result);
                root.appendChild(this.overlay);
            }

            findPlayerRoot() {
                var video = document.querySelector('video');
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

                var item = firstFunctionResult([
                    function () { return playbackManager && playbackManager.currentItem ? playbackManager.currentItem(player) : null; },
                    function () { return playbackManager && playbackManager.currentItem ? playbackManager.currentItem() : null; },
                    function () { return playbackManager && playbackManager.getCurrentItem ? playbackManager.getCurrentItem() : null; },
                    function () { return playerInfo.item || playerInfo.Item || playerInfo.currentItem || playerInfo.CurrentItem; }
                ]) || {};

                var video = document.querySelector('video');
                var position = firstFunctionResult([
                    function () { return playbackManager && playbackManager.currentTime ? playbackManager.currentTime(player) : null; },
                    function () { return playbackManager && playbackManager.getCurrentTicks ? playbackManager.getCurrentTicks() : null; },
                    function () { return playerInfo.positionTicks || playerInfo.PositionTicks; },
                    function () { return video ? video.currentTime : null; }
                ]);

                var mediaSources = item.MediaSources || item.mediaSources || [];
                var mediaSource = mediaSources.length ? mediaSources[0] : {};

                return {
                    itemId: item.Id || item.id || item.ItemId || item.itemId,
                    mediaSourceId: item.MediaSourceId || item.mediaSourceId || playerInfo.mediaSourceId || playerInfo.MediaSourceId || mediaSource.Id || mediaSource.id || null,
                    positionTicks: normalizeTicks(position, video ? video.currentTime : null),
                    audioStreamIndex: pickFirstValue([
                        playerInfo.audioStreamIndex,
                        playerInfo.AudioStreamIndex,
                        mediaSource.DefaultAudioStreamIndex,
                        mediaSource.defaultAudioStreamIndex
                    ])
                };
            }

            cacheKey(context) {
                var roundedSeconds = Math.round((context.positionTicks / TICKS_PER_SECOND) / 10) * 10;
                return context.itemId + ':' + roundedSeconds;
            }

            setBusy(isBusy) {
                if (this.button) {
                    this.button.disabled = isBusy;
                }
            }

            setResult(text) {
                if (this.result) {
                    this.result.textContent = text || '';
                }
            }

            async handleClick() {
                var context = this.getPlaybackContext();

                if (!context.itemId) {
                    this.setResult('No active item');
                    return;
                }

                var key = this.cacheKey(context);
                if (this.cache.has(key)) {
                    this.setResult(getText(this.cache.get(key)));
                    return;
                }

                this.setBusy(true);
                this.setResult('Recognizing...');

                try {
                    var response = await jsonFetch('Plugins/AuddMusicRecognition/Recognize', {
                        itemId: context.itemId,
                        mediaSourceId: context.mediaSourceId,
                        positionTicks: context.positionTicks,
                        audioStreamIndex: context.audioStreamIndex
                    });

                    this.cache.set(key, response);
                    this.setResult(getText(response));
                } catch (error) {
                    this.setResult(error && error.message ? error.message : 'Recognition failed');
                } finally {
                    this.setBusy(false);
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
