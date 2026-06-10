(function () {
    'use strict';

    var TICKS_PER_SECOND = 10000000;
    var PLUGIN_NAME = 'AuddMusicRecognitionPlugin';
    var PLUGIN_ID = 'ad3000ca-4bcb-4b4d-a67f-b9a80cd81892';
    var DEFAULT_REQUEST_TIMEOUT_MS = 45000;
    var TRACK_DISPLAY_MAX_DISTANCE_TICKS = 60 * TICKS_PER_SECOND;
    var SAME_SECOND_CLEAR_MODE = 'same-second';
    var TRANSLATIONS = {
        en: {
            recognizeMusic: 'Recognize music',
            nowPlaying: 'Now playing',
            recognition: 'Recognition',
            open: 'Open',
            copy: 'Copy',
            copied: 'Copied',
            copyHint: 'Double-click to copy',
            listeningTitle: 'Listening...',
            listeningSubtitle: 'Looking for the track in this clip',
            alreadyListeningTitle: 'Already listening...',
            alreadyListeningSubtitle: 'Just a few more seconds',
            noMatchTitle: 'No match',
            noMatchSubtitle: 'Try another moment',
            noVideoTitle: 'No video detected',
            noVideoSubtitle: 'Restart playback and try again',
            failedTitle: 'Could not recognize',
            sentPrefix: 'Sent',
            noMatch: 'No match',
            recognized: 'Recognized',
            couldNotReadPlayback: 'Could not read playback',
            noItemId: 'No item id',
            recognitionFailed: 'Recognition failed'
        },
        ru: {
            recognizeMusic: 'Распознать музыку',
            nowPlaying: 'Сейчас играет',
            recognition: 'Распознавание',
            open: 'Открыть',
            copy: 'Копировать',
            copied: 'Скопировано',
            copyHint: 'Двойной клик, чтобы скопировать',
            listeningTitle: 'Слушаю...',
            listeningSubtitle: 'Ищу трек в этом фрагменте',
            alreadyListeningTitle: 'Уже слушаю...',
            alreadyListeningSubtitle: 'Еще пару секунд',
            noMatchTitle: 'Не нашел трек',
            noMatchSubtitle: 'Попробуй другой момент',
            noVideoTitle: 'Не вижу видео',
            noVideoSubtitle: 'Открой воспроизведение заново',
            failedTitle: 'Не получилось',
            sentPrefix: 'Отправлено',
            noMatch: 'Нет совпадений',
            recognized: 'Распознано',
            couldNotReadPlayback: 'Не удалось прочитать плеер',
            noItemId: 'Не найден item id',
            recognitionFailed: 'Ошибка распознавания'
        },
        es: {
            recognizeMusic: 'Reconocer musica',
            nowPlaying: 'Reproduciendo ahora',
            recognition: 'Reconocimiento',
            open: 'Abrir',
            copy: 'Copiar',
            copied: 'Copiado',
            copyHint: 'Doble clic para copiar',
            listeningTitle: 'Escuchando...',
            listeningSubtitle: 'Buscando la pista en este fragmento',
            alreadyListeningTitle: 'Ya estoy escuchando...',
            alreadyListeningSubtitle: 'Unos segundos mas',
            noMatchTitle: 'Sin coincidencias',
            noMatchSubtitle: 'Prueba otro momento',
            noVideoTitle: 'No veo el video',
            noVideoSubtitle: 'Reinicia la reproduccion',
            failedTitle: 'No se pudo reconocer',
            sentPrefix: 'Enviado',
            noMatch: 'Sin coincidencias',
            recognized: 'Reconocido',
            couldNotReadPlayback: 'No se pudo leer la reproduccion',
            noItemId: 'Sin item id',
            recognitionFailed: 'Error de reconocimiento'
        },
        de: {
            recognizeMusic: 'Musik erkennen',
            nowPlaying: 'Lauft gerade',
            recognition: 'Erkennung',
            open: 'Offnen',
            copy: 'Kopieren',
            copied: 'Kopiert',
            copyHint: 'Doppelklick zum Kopieren',
            listeningTitle: 'Hore zu...',
            listeningSubtitle: 'Suche den Titel in diesem Ausschnitt',
            alreadyListeningTitle: 'Hore schon zu...',
            alreadyListeningSubtitle: 'Noch ein paar Sekunden',
            noMatchTitle: 'Kein Treffer',
            noMatchSubtitle: 'Versuche eine andere Stelle',
            noVideoTitle: 'Kein Video erkannt',
            noVideoSubtitle: 'Starte die Wiedergabe neu',
            failedTitle: 'Erkennung fehlgeschlagen',
            sentPrefix: 'Gesendet',
            noMatch: 'Kein Treffer',
            recognized: 'Erkannt',
            couldNotReadPlayback: 'Wiedergabe konnte nicht gelesen werden',
            noItemId: 'Keine item id',
            recognitionFailed: 'Erkennung fehlgeschlagen'
        },
        fr: {
            recognizeMusic: 'Reconnaitre la musique',
            nowPlaying: 'Lecture en cours',
            recognition: 'Reconnaissance',
            open: 'Ouvrir',
            copy: 'Copier',
            copied: 'Copie',
            copyHint: 'Double-cliquer pour copier',
            listeningTitle: 'Ecoute...',
            listeningSubtitle: 'Recherche du titre dans cet extrait',
            alreadyListeningTitle: 'Ecoute en cours...',
            alreadyListeningSubtitle: 'Encore quelques secondes',
            noMatchTitle: 'Aucun resultat',
            noMatchSubtitle: 'Essaie un autre moment',
            noVideoTitle: 'Aucune video detectee',
            noVideoSubtitle: 'Relance la lecture',
            failedTitle: 'Reconnaissance impossible',
            sentPrefix: 'Envoye',
            noMatch: 'Aucun resultat',
            recognized: 'Reconnu',
            couldNotReadPlayback: 'Lecture impossible a lire',
            noItemId: 'Aucun item id',
            recognitionFailed: 'Echec de reconnaissance'
        }
    };
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

    function normalizeLocale(locale) {
        return locale && typeof locale === 'string'
            ? locale.toLowerCase().replace('_', '-')
            : '';
    }

    function getCurrentLocale(globalize) {
        return normalizeLocale(firstFunctionResult([
            function () { return globalize && typeof globalize.getCurrentLocale === 'function' ? globalize.getCurrentLocale() : null; },
            function () { return document.documentElement ? document.documentElement.lang : null; },
            function () { return navigator.language; },
            function () { return navigator.languages && navigator.languages.length ? navigator.languages[0] : null; }
        ])) || 'en';
    }

    function createTranslator(globalize) {
        var locale = getCurrentLocale(globalize);
        var language = locale.split('-')[0];
        var strings = TRANSLATIONS[locale] || TRANSLATIONS[language] || TRANSLATIONS.en;

        return function translate(key) {
            return strings[key] || TRANSLATIONS.en[key] || key;
        };
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

    function defaultMediaSourceId(itemId) {
        var guid = normalizeGuid(String(itemId || ''));
        return guid ? guid.replace(/-/g, '') : null;
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

    function getAccessToken() {
        return firstFunctionResult([
            function () { return window.ApiClient && typeof window.ApiClient.accessToken === 'function' ? window.ApiClient.accessToken() : null; },
            function () { return window.ApiClient && typeof window.ApiClient.serverInfo === 'function' ? window.ApiClient.serverInfo().AccessToken : null; },
            function () { return window.ApiClient && window.ApiClient._serverInfo ? window.ApiClient._serverInfo.AccessToken : null; }
        ]);
    }

    function getApiHeaders(extraHeaders) {
        var headers = {
            Accept: 'application/json'
        };

        Object.keys(extraHeaders || {}).forEach(function (key) {
            headers[key] = extraHeaders[key];
        });

        var token = getAccessToken();
        if (token) {
            headers.Authorization = 'MediaBrowser Token="' + token + '"';
            headers['X-Emby-Token'] = token;
        }

        return headers;
    }

    function getApiUrl(url) {
        return firstFunctionResult([
            function () { return window.ApiClient && typeof window.ApiClient.getUrl === 'function' ? window.ApiClient.getUrl(url) : null; },
            function () { return '/' + url.replace(/^\/+/, ''); }
        ]);
    }

    function apiGetJson(url, timeoutMs) {
        var requestUrl = getApiUrl(url);

        return withTimeout(function (signal) {
            return fetch(requestUrl, {
                method: 'GET',
                headers: getApiHeaders(),
                credentials: 'same-origin',
                signal: signal
            }).then(function (response) {
                if (!response.ok) {
                    throw new Error('HTTP ' + response.status);
                }

                return response.json();
            });
        }, timeoutMs || DEFAULT_REQUEST_TIMEOUT_MS).catch(function (fetchError) {
            if (window.ApiClient && typeof window.ApiClient.ajax === 'function' && typeof window.ApiClient.getUrl === 'function') {
                return withTimeout(function () {
                    return window.ApiClient.ajax({
                        type: 'GET',
                        url: window.ApiClient.getUrl(url),
                        dataType: 'json'
                    });
                }, timeoutMs || DEFAULT_REQUEST_TIMEOUT_MS);
            }

            throw fetchError;
        });
    }

    function jsonFetch(url, payload, timeoutMs) {
        var requestUrl = getApiUrl(url);

        return withTimeout(function (signal) {
            return fetch(requestUrl, {
                method: 'POST',
                headers: getApiHeaders({
                    'Content-Type': 'application/json'
                }),
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

    function copyTextToClipboard(text) {
        if (!text) {
            return Promise.resolve(false);
        }

        if (navigator.clipboard && typeof navigator.clipboard.writeText === 'function') {
            return navigator.clipboard.writeText(text).then(function () {
                return true;
            }).catch(function () {
                return false;
            });
        }

        try {
            var input = document.createElement('textarea');
            input.value = text;
            input.setAttribute('readonly', 'readonly');
            input.style.position = 'fixed';
            input.style.left = '-9999px';
            input.style.top = '0';
            document.body.appendChild(input);
            input.select();
            var copied = document.execCommand('copy');
            input.remove();
            return Promise.resolve(copied);
        } catch (_) {
            return Promise.resolve(false);
        }
    }

    function getTrackTitle(response) {
        return readResponseValue(response, ['title', 'Title']) || '';
    }

    function getTrackArtist(response) {
        return readResponseValue(response, ['artist', 'Artist']) || '';
    }

    function getTrackAlbum(response) {
        return readResponseValue(response, ['album', 'Album']) || '';
    }

    function getAlbumArtUrl(response) {
        return readResponseValue(response, ['albumArtUrl', 'AlbumArtUrl']) || '';
    }

    function getPreferredMusicLink(response) {
        var spotifyUrl = readResponseValue(response, ['spotifyUrl', 'SpotifyUrl']);
        if (spotifyUrl) {
            return {
                label: 'Spotify',
                url: spotifyUrl
            };
        }

        var appleMusicUrl = readResponseValue(response, ['appleMusicUrl', 'AppleMusicUrl']);
        if (appleMusicUrl) {
            return {
                label: 'Apple Music',
                url: appleMusicUrl
            };
        }

        var songLink = readResponseValue(response, ['songLink', 'SongLink']);
        return songLink ? {
            label: 'AudD',
            url: songLink
        } : null;
    }

    function getTrackCopyText(response) {
        var artist = getTrackArtist(response);
        var title = getTrackTitle(response);
        return [artist, title].filter(Boolean).join(' - ') || getText(response);
    }

    function getFriendlyStatus(text, t) {
        var value = text ? String(text) : '';
        var lower = value.toLowerCase();

        if (!value || lower === 'starting...' || lower === 'recognizing...') {
            return {
                title: t('listeningTitle'),
                subtitle: t('listeningSubtitle')
            };
        }

        if (lower === 'still recognizing...') {
            return {
                title: t('alreadyListeningTitle'),
                subtitle: t('alreadyListeningSubtitle')
            };
        }

        if (lower === 'no match') {
            return {
                title: t('noMatchTitle'),
                subtitle: t('noMatchSubtitle')
            };
        }

        if (lower === 'no item id' || lower === 'could not read playback') {
            return {
                title: t('noVideoTitle'),
                subtitle: t('noVideoSubtitle')
            };
        }

        return {
            title: t('failedTitle'),
            subtitle: value
        };
    }

    function getText(response) {
        if (!response) {
            return 'No match';
        }

        var status = getResponseStatus(response);
        if (status === 'recognized') {
            return [
                getTrackArtist(response),
                getTrackTitle(response)
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

    function formatClipDebug(response, context, t) {
        var durationTicks = readResponseValue(response, ['clipDurationTicks', 'ClipDurationTicks']);
        var sizeBytes = readResponseValue(response, ['clipSizeBytes', 'ClipSizeBytes']);
        var parts = [];

        if (context && Number.isFinite(Number(context.positionTicks))) {
            parts.push('pos ' + (Number(context.positionTicks) / TICKS_PER_SECOND).toFixed(1) + ' s');
        }

        if (durationTicks !== undefined && durationTicks !== null) {
            parts.push('clip ' + (Number(durationTicks) / TICKS_PER_SECOND).toFixed(1) + ' s');
        }

        var sizeText = formatBytes(sizeBytes);
        if (sizeText) {
            parts.push(sizeText);
        }

        return parts.length ? t('sentPrefix') + ': ' + parts.join(', ') : '';
    }

    function getCurrentUserId() {
        return firstFunctionResult([
            function () { return window.ApiClient && typeof window.ApiClient.getCurrentUserId === 'function' ? window.ApiClient.getCurrentUserId() : null; },
            function () { return window.ApiClient && window.ApiClient._currentUser ? window.ApiClient._currentUser.Id || window.ApiClient._currentUser.id : null; },
            function () { return window.ApiClient && window.ApiClient.currentUser ? window.ApiClient.currentUser.Id || window.ApiClient.currentUser.id : null; },
            function () { return window.ApiClient && window.ApiClient._serverInfo ? window.ApiClient._serverInfo.UserId : null; },
            function () { return window.ApiClient && window.ApiClient._serverInfo ? window.ApiClient._serverInfo.userId : null; },
            function () { return window.ApiClient && typeof window.ApiClient.serverInfo === 'function' ? window.ApiClient.serverInfo().UserId : null; },
            function () { return window.ApiClient && typeof window.ApiClient.serverInfo === 'function' ? window.ApiClient.serverInfo().userId : null; },
            function () { return window.ApiClient && typeof window.ApiClient.getCurrentUser === 'function' ? window.ApiClient.getCurrentUser() : null; },
            function () { return window.ApiClient && typeof window.ApiClient.getCurrentUser === 'function' ? window.ApiClient.getCurrentUser().Id : null; },
            function () { return window.ApiClient && typeof window.ApiClient.getCurrentUser === 'function' ? window.ApiClient.getCurrentUser().id : null; },
            function () { return window.ApiClient && window.ApiClient._serverInfo ? window.ApiClient._serverInfo.UserId : null; },
            function () { return window.ApiClient && window.ApiClient._serverInfo ? window.ApiClient._serverInfo.userId : null; }
        ]);
    }

    function getUserIdFromValue(value) {
        if (!value) {
            return null;
        }

        if (typeof value === 'string') {
            return value;
        }

        if (value.Id || value.id || value.UserId || value.userId) {
            return value.Id || value.id || value.UserId || value.userId;
        }

        return null;
    }

    function normalizeIdForCompare(value) {
        return value === undefined || value === null
            ? ''
            : String(value).replace(/-/g, '').toLowerCase();
    }

    function areIdsEqual(first, second) {
        var normalizedFirst = normalizeIdForCompare(first);
        var normalizedSecond = normalizeIdForCompare(second);
        return normalizedFirst && normalizedSecond && normalizedFirst === normalizedSecond;
    }

    function getCurrentUserIdAsync() {
        var value = getCurrentUserId();
        if (value && typeof value.then === 'function') {
            return value.then(getUserIdFromValue).catch(function () {
                return null;
            });
        }

        var userId = getUserIdFromValue(value);
        if (userId) {
            return Promise.resolve(userId);
        }

        if (window.ApiClient && typeof window.ApiClient.getCurrentUser === 'function') {
            try {
                var user = window.ApiClient.getCurrentUser();
                if (user && typeof user.then === 'function') {
                    return user.then(getUserIdFromValue).catch(function () {
                        return null;
                    });
                }

                return Promise.resolve(getUserIdFromValue(user));
            } catch (_) {
            }
        }

        return Promise.resolve(null);
    }

    function getDeviceIdFromValue(value) {
        if (!value) {
            return null;
        }

        if (typeof value === 'string') {
            return value;
        }

        return value.DeviceId || value.deviceId || value.Id || value.id || null;
    }

    function getCurrentDeviceIdAsync() {
        var apiClient = window.ApiClient;
        var value = firstFunctionResult([
            function () { return apiClient && typeof apiClient.deviceId === 'function' ? apiClient.deviceId() : null; },
            function () { return apiClient && typeof apiClient.getDeviceId === 'function' ? apiClient.getDeviceId() : null; },
            function () { return apiClient ? apiClient._deviceId || apiClient.deviceId : null; },
            function () { return apiClient && apiClient._serverInfo ? apiClient._serverInfo.DeviceId || apiClient._serverInfo.deviceId : null; },
            function () { return apiClient && typeof apiClient.serverInfo === 'function' ? apiClient.serverInfo() : null; }
        ]);

        if (value && typeof value.then === 'function') {
            return value.then(getDeviceIdFromValue).catch(function () {
                return null;
            });
        }

        return Promise.resolve(getDeviceIdFromValue(value));
    }

    function readArrayValue(value, names) {
        for (var i = 0; i < names.length; i += 1) {
            if (value && Array.isArray(value[names[i]])) {
                return value[names[i]];
            }
        }

        return [];
    }

    function readObjectValue(value, names) {
        for (var i = 0; i < names.length; i += 1) {
            if (value && value[names[i]] && typeof value[names[i]] === 'object') {
                return value[names[i]];
            }
        }

        return null;
    }

    function isLocalMediaPath(path) {
        return typeof path === 'string'
            && path.length > 0
            && !/^https?:\/\//i.test(path)
            && !/^rtmps?:\/\//i.test(path);
    }

    function getPlaybackInfoUrl(itemId, userId) {
        var query = userId && typeof userId !== 'object' ? '?userId=' + encodeURIComponent(String(userId)) : '';
        return 'Items/' + encodeURIComponent(itemId) + '/PlaybackInfo' + query;
    }

    function getSessionsUrl(userId, deviceId) {
        var query = ['activeWithinSeconds=60'];
        if (userId && typeof userId !== 'object') {
            query.unshift('controllableByUserId=' + encodeURIComponent(String(userId)));
        }

        if (deviceId && typeof deviceId !== 'object') {
            query.unshift('deviceId=' + encodeURIComponent(String(deviceId)));
        }

        return 'Sessions?' + query.join('&');
    }

    function choosePlaybackMediaSource(context, playbackInfo) {
        var mediaSources = readArrayValue(playbackInfo, ['MediaSources', 'mediaSources']);
        if (!mediaSources.length) {
            return null;
        }

        var requestedId = context.mediaSourceId;
        var selected = null;

        if (requestedId) {
            selected = mediaSources.filter(function (source) {
                var id = readResponseValue(source, ['Id', 'id']);
                return id && String(id).toLowerCase() === String(requestedId).toLowerCase();
            })[0] || null;
        }

        return selected || mediaSources.filter(function (source) {
            return isLocalMediaPath(readResponseValue(source, ['Path', 'path']));
        })[0] || mediaSources[0];
    }

    function mergePlaybackInfo(context, playbackInfo) {
        var mediaSource = choosePlaybackMediaSource(context, playbackInfo);
        if (!mediaSource) {
            return context;
        }

        var mediaSourceId = readResponseValue(mediaSource, ['Id', 'id']);
        var mediaSourcePath = readResponseValue(mediaSource, ['Path', 'path']);
        var defaultAudioStreamIndex = readResponseValue(mediaSource, ['DefaultAudioStreamIndex', 'defaultAudioStreamIndex']);
        var mediaStreams = readArrayValue(mediaSource, ['MediaStreams', 'mediaStreams']);
        var audioStream = mediaStreams.filter(function (stream) {
            var type = readResponseValue(stream, ['Type', 'type']);
            return type && String(type).toLowerCase() === 'audio';
        })[0] || null;

        context.mediaSourceId = mediaSourceId || context.mediaSourceId;
        context.mediaSourcePath = isLocalMediaPath(mediaSourcePath) ? mediaSourcePath : context.mediaSourcePath;
        context.audioStreamIndex = pickFirstValue([
            context.audioStreamIndex,
            defaultAudioStreamIndex,
            audioStream ? readResponseValue(audioStream, ['Index', 'index']) : null
        ]);

        return context;
    }

    function choosePlaybackSession(context, sessions, userId, deviceId) {
        var values = Array.isArray(sessions) ? sessions : [];
        var candidates = values.map(function (session, index) {
            var playState = readObjectValue(session, ['PlayState', 'playState']) || {};
            var nowPlayingItem = readObjectValue(session, ['NowPlayingItem', 'nowPlayingItem']) || {};
            var sessionItemId = readResponseValue(session, ['ItemId', 'itemId'])
                || readResponseValue(nowPlayingItem, ['Id', 'id', 'ItemId', 'itemId']);
            var sessionMediaSourceId = readResponseValue(playState, ['MediaSourceId', 'mediaSourceId'])
                || readResponseValue(session, ['MediaSourceId', 'mediaSourceId']);
            var sessionDeviceId = readResponseValue(session, ['DeviceId', 'deviceId']);
            var sessionUserId = readResponseValue(session, ['UserId', 'userId']);
            var lastPlaybackCheckIn = readResponseValue(session, ['LastPlaybackCheckIn', 'lastPlaybackCheckIn']);
            var score = index;

            if (!sessionItemId) {
                return null;
            }

            var deviceMatch = Boolean(deviceId && areIdsEqual(sessionDeviceId, deviceId));
            var itemMatch = Boolean(context && context.itemId && areIdsEqual(sessionItemId, context.itemId));
            var mediaSourceMatch = Boolean(context && context.mediaSourceId
                && sessionMediaSourceId
                && areIdsEqual(sessionMediaSourceId, context.mediaSourceId));

            if (deviceMatch) {
                score += 10000;
            }

            if (userId && areIdsEqual(sessionUserId, userId)) {
                score += 2000;
            }

            if (itemMatch) {
                score += 500;
            }

            if (mediaSourceMatch) {
                score += 250;
            }

            var checkInTime = Date.parse(lastPlaybackCheckIn || '');
            if (Number.isFinite(checkInTime)) {
                score += checkInTime / 10000000000000;
            }

            return {
                session: session,
                score: score,
                deviceMatch: deviceMatch,
                itemMatch: itemMatch,
                mediaSourceMatch: mediaSourceMatch
            };
        }).filter(Boolean).sort(function (first, second) {
            return second.score - first.score;
        });

        var exactDeviceCandidates = candidates.filter(function (candidate) {
            return candidate.deviceMatch;
        });
        if (exactDeviceCandidates.length) {
            return exactDeviceCandidates[0].session;
        }

        var localContextCandidates = candidates.filter(function (candidate) {
            return candidate.itemMatch || candidate.mediaSourceMatch;
        });
        if (localContextCandidates.length) {
            return localContextCandidates[0].session;
        }

        // Never replace a valid local player context with another device's session.
        if (context && context.itemId) {
            return null;
        }

        return candidates.length === 1 ? candidates[0].session : null;
    }

    function mergeSessionPlaybackContext(context, session) {
        if (!session) {
            return context;
        }

        var playState = readObjectValue(session, ['PlayState', 'playState']) || {};
        var nowPlayingItem = readObjectValue(session, ['NowPlayingItem', 'nowPlayingItem']) || {};
        var sessionItemId = readResponseValue(session, ['ItemId', 'itemId'])
            || readResponseValue(nowPlayingItem, ['Id', 'id', 'ItemId', 'itemId']);
        var sessionMediaSourceId = readResponseValue(playState, ['MediaSourceId', 'mediaSourceId'])
            || readResponseValue(session, ['MediaSourceId', 'mediaSourceId']);
        var sessionAudioStreamIndex = pickFirstValue([
            readResponseValue(playState, ['AudioStreamIndex', 'audioStreamIndex']),
            readResponseValue(session, ['AudioStreamIndex', 'audioStreamIndex'])
        ]);
        var itemPath = readResponseValue(nowPlayingItem, ['Path', 'path']);

        if (sessionItemId && !areIdsEqual(context.itemId, sessionItemId)) {
            context.itemId = sessionItemId;
            context.mediaSourceId = null;
            context.mediaSourcePath = null;
            context.audioStreamIndex = null;
        } else if (sessionMediaSourceId && context.mediaSourceId && !areIdsEqual(context.mediaSourceId, sessionMediaSourceId)) {
            context.mediaSourcePath = null;
            context.audioStreamIndex = null;
        }

        context.mediaSourceId = sessionMediaSourceId || context.mediaSourceId;
        context.audioStreamIndex = pickFirstValue([
            sessionAudioStreamIndex,
            context.audioStreamIndex
        ]);

        mergePlaybackInfo(context, nowPlayingItem);

        if (!context.mediaSourcePath && isLocalMediaPath(itemPath)) {
            context.mediaSourcePath = itemPath;
        }

        return context;
    }

    function fetchPlaybackSessions(userId, deviceId) {
        var urls = [
            getSessionsUrl(userId, deviceId),
            getSessionsUrl(userId, null),
            getSessionsUrl(null, null)
        ].filter(function (value, index, array) {
            return array.indexOf(value) === index;
        });

        function tryNext(index, lastError) {
            if (index >= urls.length) {
                return lastError ? Promise.reject(lastError) : Promise.resolve([]);
            }

            return apiGetJson(urls[index], 6000).then(function (sessions) {
                return Array.isArray(sessions) && sessions.length
                    ? sessions
                    : tryNext(index + 1, lastError);
            }).catch(function (error) {
                return tryNext(index + 1, error);
            });
        }

        return tryNext(0, null);
    }

    function getActiveVideo() {
        var videos = asArray(document.querySelectorAll('video'));
        var pictureInPictureVideo = document.pictureInPictureElement;

        function scoreVideo(video, index) {
            var score = index;
            var rect;
            var style;

            if (video === pictureInPictureVideo) {
                score += 100000;
            }

            try {
                rect = video.getBoundingClientRect();
                style = window.getComputedStyle(video);
                if (rect.width > 1 && rect.height > 1 && style.display !== 'none' && style.visibility !== 'hidden') {
                    score += 10000 + Math.min((rect.width * rect.height) / 1000, 1000);
                }
            } catch (_) {
            }

            if (!video.paused) {
                score += 5000;
            }

            if (video.currentSrc || video.src) {
                score += 1000;
            }

            if (video.readyState >= 1) {
                score += 500;
            }

            if (video.networkState !== 3) {
                score += 100;
            }

            if (!video.ended) {
                score += 50;
            }

            return score;
        }

        return videos.map(function (video, index) {
            return { video: video, score: scoreVideo(video, index) };
        }).sort(function (first, second) {
            return second.score - first.score;
        }).map(function (entry) {
            return entry.video;
        })[0] || null;
    }

    function collectPlaybackUrls(video) {
        var directUrls = [];

        if (video) {
            directUrls.push(video.currentSrc, video.src);
            asArray(video.querySelectorAll('source')).forEach(function (source) {
                directUrls.push(source.src);
            });
        }

        directUrls.push(window.location.href);

        return directUrls.filter(function (value, index, array) {
            return value && array.indexOf(value) === index;
        });
    }

    var playbackElementSequence = 0;

    function getVideoPlaybackIdentity(video) {
        if (!video) {
            return null;
        }

        if (!video.__auddPlaybackElementId) {
            playbackElementSequence += 1;
            video.__auddPlaybackElementId = playbackElementSequence;
        }

        return [video.__auddPlaybackElementId, video.currentSrc || video.src || ''].join(':');
    }

    function addParamsFromQuery(params, query) {
        if (!query) {
            return;
        }

        try {
            var search = query.charAt(0) === '?' ? query : '?' + query;
            var queryParams = new URLSearchParams(search);
            queryParams.forEach(function (value, key) {
                if (!params.has(key)) {
                    params.set(key, value);
                }
            });
        } catch (_) {
        }
    }

    function getAllParams(parsed) {
        var params = new URLSearchParams(parsed.search || '');
        var hash = parsed.hash || '';
        var hashQueryIndex = hash.indexOf('?');

        if (hashQueryIndex >= 0) {
            addParamsFromQuery(params, hash.slice(hashQueryIndex + 1));
        } else if (hash.indexOf('=') >= 0) {
            addParamsFromQuery(params, hash.replace(/^#/, ''));
        }

        return params;
    }

    function parsePlaybackUrl(url) {
        if (!url || typeof url !== 'string') {
            return {};
        }

        var result = {};

        try {
            var parsed = new URL(url, window.location.href);
            var params = getAllParams(parsed);
            var itemId = normalizeGuid(
                params.get('ItemId')
                || params.get('itemId')
                || params.get('Id')
                || params.get('id'));

            if (!itemId) {
                var pathMatch = parsed.pathname.match(/\/(?:Videos|Items|Audio)\/([0-9a-f-]{32,36})(?:\/|$)/i);
                itemId = pathMatch ? normalizeGuid(pathMatch[1]) : null;
            }

            if (itemId) {
                result.itemId = itemId;
            }

            result.mediaSourceId = params.get('MediaSourceId') || params.get('mediaSourceId') || null;
            result.audioStreamIndex = parseNumber(params.get('AudioStreamIndex') || params.get('audioStreamIndex'));
            result.positionTicks = parseNumber(params.get('StartTimeTicks') || params.get('startTimeTicks'));
        } catch (_) {
        }

        return result;
    }

    function getPlaybackContextFromUrls(video) {
        var urls = collectPlaybackUrls(video);
        var result = {};

        for (var i = 0; i < urls.length; i += 1) {
            var parsed = parsePlaybackUrl(urls[i]);
            if (result.itemId && parsed.itemId && result.itemId !== parsed.itemId) {
                continue;
            }

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
                this.t = createTranslator(this.dependencies.globalize || window.Globalize || window.globalize);
                this.cache = new Map();
                this.overlay = null;
                this.button = null;
                this.panel = null;
                this.heading = null;
                this.card = null;
                this.cover = null;
                this.coverImage = null;
                this.title = null;
                this.artist = null;
                this.album = null;
                this.actions = null;
                this.serviceLink = null;
                this.openLink = null;
                this.copyButton = null;
                this.debug = null;
                this.copyText = '';
                this.lastResponse = null;
                this.lastResponseContext = null;
                this.lastStatusText = '';
                this.lastStatusContext = null;
                this.lastStatusClearMode = null;
                this.currentRoot = null;
                this.currentVideo = null;
                this.currentVideoPlaybackIdentity = null;
                this.currentMediaIdentity = null;
                this.currentItemId = null;
                this.lastTriggerAt = 0;
                this.requestSequence = 0;
                this.isBusy = false;
                this.settings = {
                    showDebugInfo: false,
                    requestTimeoutMs: DEFAULT_REQUEST_TIMEOUT_MS
                };
                this.ensureOverlay = this.ensureOverlay.bind(this);
                this.handleTrigger = this.handleTrigger.bind(this);
                this.handleCopyText = this.handleCopyText.bind(this);
                this.handlePlaybackPositionChange = this.handlePlaybackPositionChange.bind(this);
                this.stopOverlayEvent = this.stopOverlayEvent.bind(this);
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

                this.bindVideoEvents(null);

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
                    '.auddRecognitionOverlay{position:fixed;top:calc(env(safe-area-inset-top,0px) + 16px);right:calc(env(safe-area-inset-right,0px) + 126px);z-index:99999;display:flex;flex-direction:column;align-items:flex-end;gap:8px;max-width:min(430px,calc(100vw - 150px));pointer-events:none;font-family:inherit;color:#fff;}',
                    '.auddRecognitionButton{pointer-events:auto;width:42px;height:42px;border:0;border-radius:50%;display:inline-flex;align-items:center;justify-content:center;background:rgba(20,20,20,.72);color:#fff;box-shadow:0 8px 26px rgba(0,0,0,.34);cursor:pointer;touch-action:manipulation;transition:background .18s ease,transform .18s ease,opacity .18s ease;}',
                    '.auddRecognitionButton:hover{background:rgba(34,34,34,.86);transform:translateY(-1px);}',
                    '.auddRecognitionButton:disabled{opacity:.62;cursor:default;}',
                    '.auddRecognitionButton.is-busy svg{animation:auddRecognitionPulse 1.1s ease-in-out infinite;}',
                    '.auddRecognitionButton svg{width:21px;height:21px;fill:currentColor;}',
                    '.auddRecognitionPanel{pointer-events:auto;width:min(412px,calc(100vw - 32px));text-shadow:0 1px 2px rgba(0,0,0,.4);}',
                    '.auddRecognitionPanel.is-hidden{display:none;}',
                    '.auddRecognitionHeading{margin:0 0 9px 0;font-size:18px;font-weight:700;line-height:1.2;color:#fff;}',
                    '.auddRecognitionCard{border-radius:8px;background:rgba(24,24,24,.88);box-shadow:0 14px 42px rgba(0,0,0,.42);padding:18px 20px 16px;backdrop-filter:blur(18px);-webkit-backdrop-filter:blur(18px);}',
                    '.auddRecognitionMain{display:grid;grid-template-columns:58px minmax(0,1fr);gap:14px;align-items:center;}',
                    '.auddRecognitionCover{width:58px;height:58px;border-radius:6px;background:linear-gradient(135deg,rgba(255,255,255,.18),rgba(255,255,255,.06));display:flex;align-items:center;justify-content:center;overflow:hidden;box-shadow:inset 0 0 0 1px rgba(255,255,255,.08);}',
                    '.auddRecognitionCover img{width:100%;height:100%;object-fit:cover;display:none;}',
                    '.auddRecognitionCover.has-image img{display:block;}',
                    '.auddRecognitionCover.has-image .auddRecognitionCoverIcon{display:none;}',
                    '.auddRecognitionCoverIcon{width:27px;height:27px;color:rgba(255,255,255,.72);}',
                    '.auddRecognitionTitle{font-size:20px;font-weight:800;line-height:1.15;color:#fff;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;user-select:text;-webkit-user-select:text;}',
                    '.auddRecognitionArtist{margin-top:6px;font-size:16px;line-height:1.2;color:rgba(255,255,255,.72);overflow:hidden;text-overflow:ellipsis;white-space:nowrap;user-select:text;-webkit-user-select:text;}',
                    '.auddRecognitionAlbum{margin-top:5px;font-size:12px;line-height:1.2;color:rgba(255,255,255,.48);overflow:hidden;text-overflow:ellipsis;white-space:nowrap;user-select:text;-webkit-user-select:text;}',
                    '.auddRecognitionAlbum:empty{display:none;}',
                    '.auddRecognitionActions{margin-top:16px;display:flex;align-items:stretch;width:100%;min-height:40px;border-radius:999px;background:rgba(255,255,255,.12);overflow:hidden;}',
                    '.auddRecognitionActions.is-hidden{display:none;}',
                    '.auddRecognitionAction{appearance:none;-webkit-appearance:none;box-sizing:border-box;flex:1 1 0;min-width:84px;border:0;border-radius:0;background:transparent;color:#fff;text-decoration:none;min-height:40px;padding:0 16px;display:inline-flex;align-items:center;justify-content:center;font-size:13px;font-weight:700;line-height:1;white-space:nowrap;cursor:pointer;font-family:inherit;}',
                    '.auddRecognitionAction:hover{background:rgba(255,255,255,.1);text-decoration:none;}',
                    '.auddRecognitionAction + .auddRecognitionAction{border-left:1px solid rgba(255,255,255,.2);}',
                    '.auddRecognitionStatus .auddRecognitionTitle{font-size:17px;font-weight:750;}',
                    '.auddRecognitionStatus .auddRecognitionArtist{font-size:13px;}',
                    '.auddRecognitionStatus .auddRecognitionActions{display:none;}',
                    '.auddRecognitionDebug{pointer-events:auto;margin-top:7px;min-height:24px;max-width:100%;padding:6px 8px;border-radius:7px;background:rgba(20,20,20,.55);color:rgba(255,255,255,.82);font-size:11px;line-height:1.2;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;cursor:text;user-select:text;-webkit-user-select:text;}',
                    '.auddRecognitionDebug:empty{display:none;}',
                    '@keyframes auddRecognitionPulse{0%,100%{transform:scale(1);opacity:1;}50%{transform:scale(.9);opacity:.72;}}',
                    '@media (max-width: 760px) and (pointer: coarse){.auddRecognitionOverlay{top:8px;right:76px;gap:4px;max-width:min(216px,calc(100vw - 94px));}.auddRecognitionButton{width:38px;height:38px;}.auddRecognitionButton svg{width:19px;height:19px;}.auddRecognitionPanel{width:min(216px,calc(100vw - 94px));}.auddRecognitionHeading{margin-bottom:4px;font-size:11px;}.auddRecognitionCard{padding:7px 8px 8px;}.auddRecognitionMain{grid-template-columns:30px minmax(0,1fr);gap:7px;}.auddRecognitionCover{width:30px;height:30px;border-radius:4px;}.auddRecognitionCoverIcon{width:16px;height:16px;}.auddRecognitionTitle{font-size:12px;line-height:1.1;}.auddRecognitionArtist{margin-top:3px;font-size:10px;}.auddRecognitionAlbum{margin-top:2px;font-size:9px;}.auddRecognitionActions{margin-top:7px;min-height:27px;}.auddRecognitionAction{min-width:45px;min-height:27px;padding:0 5px;font-size:9px;}.auddRecognitionStatus .auddRecognitionTitle{font-size:11px;}.auddRecognitionStatus .auddRecognitionArtist{font-size:9px;}.auddRecognitionDebug{margin-top:4px;min-height:17px;padding:3px 5px;font-size:8px;}}'
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
                var video = getActiveVideo();
                this.bindVideoEvents(video);

                if (!video || !this.isPauseMenuAvailable(video)) {
                    if (this.overlay) {
                        this.overlay.remove();
                        this.overlay = null;
                    }

                    this.currentRoot = null;
                    return;
                }

                var playerRoot = this.findPlayerRoot(video);

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
                this.button.title = this.t('recognizeMusic');
                this.button.setAttribute('aria-label', this.t('recognizeMusic'));
                this.button.innerHTML = '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 3v10.55A4 4 0 1 0 14 17V7h4V3h-6Z"/></svg>';
                this.button.addEventListener('click', this.handleTrigger, true);
                this.button.addEventListener('pointerup', this.handleTrigger, true);

                this.panel = document.createElement('div');
                this.panel.className = 'auddRecognitionPanel is-hidden';
                this.panel.setAttribute('aria-live', 'polite');
                this.panel.title = this.t('copyHint');
                this.panel.addEventListener('dblclick', this.handleCopyText, true);
                this.panel.addEventListener('pointerdown', this.stopOverlayEvent);
                this.panel.addEventListener('click', this.stopOverlayEvent);

                this.heading = document.createElement('div');
                this.heading.className = 'auddRecognitionHeading';
                this.heading.textContent = this.t('nowPlaying');

                this.card = document.createElement('div');
                this.card.className = 'auddRecognitionCard';

                var main = document.createElement('div');
                main.className = 'auddRecognitionMain';

                this.cover = document.createElement('div');
                this.cover.className = 'auddRecognitionCover';
                this.cover.innerHTML = '<img alt=""><svg class="auddRecognitionCoverIcon" viewBox="0 0 24 24" aria-hidden="true"><path fill="currentColor" d="M12 3v10.55A4 4 0 1 0 14 17V7h4V3h-6Z"/></svg>';
                this.coverImage = this.cover.querySelector('img');
                this.coverImage.addEventListener('error', function () {
                    this.setCover('');
                }.bind(this));

                var trackText = document.createElement('div');
                trackText.className = 'auddRecognitionText';

                this.title = document.createElement('div');
                this.title.className = 'auddRecognitionTitle';

                this.artist = document.createElement('div');
                this.artist.className = 'auddRecognitionArtist';

                this.album = document.createElement('div');
                this.album.className = 'auddRecognitionAlbum';

                trackText.appendChild(this.title);
                trackText.appendChild(this.artist);
                trackText.appendChild(this.album);
                main.appendChild(this.cover);
                main.appendChild(trackText);

                this.actions = document.createElement('div');
                this.actions.className = 'auddRecognitionActions is-hidden';

                this.serviceLink = document.createElement('a');
                this.serviceLink.className = 'auddRecognitionAction';
                this.serviceLink.target = '_blank';
                this.serviceLink.rel = 'noopener noreferrer';
                this.serviceLink.addEventListener('click', this.stopOverlayEvent);

                this.openLink = document.createElement('a');
                this.openLink.className = 'auddRecognitionAction';
                this.openLink.target = '_blank';
                this.openLink.rel = 'noopener noreferrer';
                this.openLink.textContent = this.t('open');
                this.openLink.addEventListener('click', this.stopOverlayEvent);

                this.copyButton = document.createElement('button');
                this.copyButton.type = 'button';
                this.copyButton.className = 'auddRecognitionAction';
                this.copyButton.textContent = this.t('copy');
                this.copyButton.addEventListener('click', this.handleCopyText, true);

                this.actions.appendChild(this.serviceLink);
                this.actions.appendChild(this.openLink);
                this.actions.appendChild(this.copyButton);
                this.card.appendChild(main);
                this.card.appendChild(this.actions);
                this.panel.appendChild(this.heading);
                this.panel.appendChild(this.card);

                this.debug = document.createElement('div');
                this.debug.className = 'auddRecognitionDebug';
                this.debug.title = this.t('copyHint');
                this.debug.addEventListener('dblclick', this.handleCopyText, true);
                this.debug.addEventListener('pointerdown', this.stopOverlayEvent);
                this.debug.addEventListener('click', this.stopOverlayEvent);

                this.overlay.appendChild(this.button);
                this.overlay.appendChild(this.panel);
                this.overlay.appendChild(this.debug);
                root.appendChild(this.overlay);

                this.setBusy(this.isBusy);
                this.syncPlaybackIdentity();
                if (this.lastResponse) {
                    this.setRecognitionResponse(this.lastResponse);
                } else if (this.lastStatusText) {
                    this.setResult(this.lastStatusText, this.lastStatusContext, this.lastStatusClearMode);
                }
            }

            bindVideoEvents(video) {
                if (this.currentVideo === video) {
                    return;
                }

                if (this.currentVideo) {
                    this.currentVideo.removeEventListener('play', this.ensureOverlay);
                    this.currentVideo.removeEventListener('playing', this.ensureOverlay);
                    this.currentVideo.removeEventListener('pause', this.ensureOverlay);
                    this.currentVideo.removeEventListener('ended', this.ensureOverlay);
                    this.currentVideo.removeEventListener('emptied', this.ensureOverlay);
                    this.currentVideo.removeEventListener('loadstart', this.ensureOverlay);
                    this.currentVideo.removeEventListener('durationchange', this.ensureOverlay);
                    this.currentVideo.removeEventListener('canplay', this.ensureOverlay);
                    this.currentVideo.removeEventListener('loadedmetadata', this.ensureOverlay);
                    this.currentVideo.removeEventListener('seeking', this.handlePlaybackPositionChange);
                    this.currentVideo.removeEventListener('seeked', this.handlePlaybackPositionChange);
                    this.currentVideo.removeEventListener('timeupdate', this.handlePlaybackPositionChange);
                }

                this.currentVideo = video || null;

                if (this.currentVideo) {
                    this.currentVideo.addEventListener('play', this.ensureOverlay);
                    this.currentVideo.addEventListener('playing', this.ensureOverlay);
                    this.currentVideo.addEventListener('pause', this.ensureOverlay);
                    this.currentVideo.addEventListener('ended', this.ensureOverlay);
                    this.currentVideo.addEventListener('emptied', this.ensureOverlay);
                    this.currentVideo.addEventListener('loadstart', this.ensureOverlay);
                    this.currentVideo.addEventListener('durationchange', this.ensureOverlay);
                    this.currentVideo.addEventListener('canplay', this.ensureOverlay);
                    this.currentVideo.addEventListener('loadedmetadata', this.ensureOverlay);
                    this.currentVideo.addEventListener('seeking', this.handlePlaybackPositionChange);
                    this.currentVideo.addEventListener('seeked', this.handlePlaybackPositionChange);
                    this.currentVideo.addEventListener('timeupdate', this.handlePlaybackPositionChange);
                }
            }

            handlePlaybackPositionChange(event) {
                var video = event && event.currentTarget ? event.currentTarget : this.currentVideo;
                if (!video || !Number.isFinite(Number(video.currentTime))) {
                    return;
                }

                var referenceContext = this.lastResponseContext || this.lastStatusContext;
                if (!referenceContext) {
                    return;
                }

                var context = {
                    itemId: referenceContext.itemId,
                    mediaSourceId: referenceContext.mediaSourceId || '',
                    positionTicks: Math.round(Number(video.currentTime) * TICKS_PER_SECOND)
                };

                if (this.isRecognitionDisplayStale(context)) {
                    this.setResult('');
                    this.setDebug('');
                }
            }

            isPauseMenuAvailable(video) {
                if (!video) {
                    return false;
                }

                if (!video.paused && !video.ended) {
                    return false;
                }

                var playbackPaused = this.getPlaybackPausedState();
                if (playbackPaused === true) {
                    return true;
                }

                if (playbackPaused === false) {
                    return false;
                }

                return video.paused || video.ended;
            }

            getPlaybackPausedState() {
                var playbackManager = this.dependencies.playbackManager || window.playbackManager || window.PlaybackManager;
                var player = firstFunctionResult([
                    function () { return playbackManager && playbackManager.getCurrentPlayer ? playbackManager.getCurrentPlayer() : null; },
                    function () { return playbackManager && playbackManager.currentPlayer ? playbackManager.currentPlayer() : null; }
                ]);

                var playerInfo = firstFunctionResult([
                    function () { return playbackManager && playbackManager.getPlayerInfo ? playbackManager.getPlayerInfo() : null; },
                    function () { return playbackManager && playbackManager.getPlayerState ? playbackManager.getPlayerState() : null; }
                ]) || {};

                var playState = playerInfo.PlayState || playerInfo.playState || {};
                var paused = pickFirstValue([
                    playState.IsPaused,
                    playState.isPaused,
                    playerInfo.IsPaused,
                    playerInfo.isPaused,
                    player && typeof player.paused === 'boolean' ? player.paused : null,
                    firstFunctionResult([
                        function () { return playbackManager && typeof playbackManager.paused === 'function' ? playbackManager.paused(player) : null; },
                        function () { return playbackManager && typeof playbackManager.isPaused === 'function' ? playbackManager.isPaused(player) : null; }
                    ])
                ]);

                if (typeof paused === 'boolean') {
                    return paused;
                }

                if (typeof paused === 'string') {
                    return paused.toLowerCase() === 'true';
                }

                return null;
            }

            findPlayerRoot(video) {
                video = video || getActiveVideo();
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
                    function () { return videoPosition; },
                    function () { return statePlayState.PositionTicks || statePlayState.positionTicks; },
                    function () { return playbackManager && playbackManager.currentTime ? playbackManager.currentTime(player) : null; },
                    function () { return playbackManager && playbackManager.getCurrentTicks ? playbackManager.getCurrentTicks() : null; },
                    function () { return playerInfo.positionTicks || playerInfo.PositionTicks; },
                    function () { return fallbackPosition; },
                    function () { return video ? video.currentTime : null; }
                ]);

                var mediaSources = item.MediaSources || item.mediaSources || [];
                var mediaSource = currentMediaSource || (mediaSources.length ? mediaSources[0] : {});
                var managerItemId = item.Id || item.id || item.ItemId || item.itemId;
                var itemId = fallback.itemId || managerItemId;
                var managerContextIsCurrent = !fallback.itemId
                    || !managerItemId
                    || areIdsEqual(fallback.itemId, managerItemId);
                var mediaSourcePath = managerContextIsCurrent
                    ? readResponseValue(mediaSource, ['Path', 'path']) || readResponseValue(item, ['Path', 'path'])
                    : null;
                var mediaSourceId = fallback.mediaSourceId
                    || (managerContextIsCurrent ? item.MediaSourceId : null)
                    || (managerContextIsCurrent ? item.mediaSourceId : null)
                    || (managerContextIsCurrent ? playerInfo.mediaSourceId : null)
                    || (managerContextIsCurrent ? playerInfo.MediaSourceId : null)
                    || (managerContextIsCurrent ? mediaSource.Id : null)
                    || (managerContextIsCurrent ? mediaSource.id : null)
                    || defaultMediaSourceId(itemId);

                return {
                    itemId: itemId,
                    mediaSourceId: mediaSourceId || null,
                    mediaSourcePath: isLocalMediaPath(mediaSourcePath) ? mediaSourcePath : null,
                    positionTicks: normalizeTicks(position, video ? video.currentTime : null),
                    audioStreamIndex: pickFirstValue([
                        fallback.audioStreamIndex,
                        managerContextIsCurrent ? statePlayState.AudioStreamIndex : null,
                        managerContextIsCurrent ? statePlayState.audioStreamIndex : null,
                        managerContextIsCurrent ? playerInfo.audioStreamIndex : null,
                        managerContextIsCurrent ? playerInfo.AudioStreamIndex : null,
                        managerContextIsCurrent ? mediaSource.DefaultAudioStreamIndex : null,
                        managerContextIsCurrent ? mediaSource.defaultAudioStreamIndex : null,
                        managerContextIsCurrent ? mediaSource.AudioStreamIndex : null,
                        managerContextIsCurrent ? mediaSource.audioStreamIndex : null
                    ])
                };
            }

            async enrichPlaybackContext(context) {
                if (!context) {
                    return context;
                }

                var identity = await Promise.all([
                    getCurrentUserIdAsync(),
                    getCurrentDeviceIdAsync()
                ]);
                var userId = identity[0];
                var deviceId = identity[1];

                try {
                    var sessions = await fetchPlaybackSessions(userId, deviceId);
                    mergeSessionPlaybackContext(
                        context,
                        choosePlaybackSession(context, sessions, userId, deviceId));
                } catch (_) {
                }

                if (!context.itemId) {
                    return context;
                }

                try {
                    var playbackInfo = await apiGetJson(getPlaybackInfoUrl(context.itemId, userId), 6000);
                    mergePlaybackInfo(context, playbackInfo);
                } catch (_) {
                }

                return context;
            }

            cacheKey(context) {
                var roundedSeconds = Math.round((context.positionTicks / TICKS_PER_SECOND) / 10) * 10;
                return [context.itemId, context.mediaSourceId || '', roundedSeconds].join(':');
            }

            mediaIdentity(context) {
                if (!context || !context.itemId) {
                    return null;
                }

                return [context.itemId, context.mediaSourceId || ''].join(':');
            }

            syncPlaybackIdentity() {
                var context;
                var video = getActiveVideo();
                var videoPlaybackIdentity = getVideoPlaybackIdentity(video);

                if (!this.currentVideoPlaybackIdentity) {
                    this.currentVideoPlaybackIdentity = videoPlaybackIdentity;
                } else if (videoPlaybackIdentity && this.currentVideoPlaybackIdentity !== videoPlaybackIdentity) {
                    this.currentVideoPlaybackIdentity = videoPlaybackIdentity;
                    this.currentMediaIdentity = null;
                    this.currentItemId = null;
                    this.requestSequence += 1;
                    this.setBusy(false);
                    this.setResult('');
                    this.setDebug('');
                }

                try {
                    context = this.getPlaybackContext();
                } catch (_) {
                    return;
                }

                var identity = this.mediaIdentity(context);
                var directContext = getPlaybackContextFromUrls(video);
                var hasReliableItemId = Boolean(directContext.itemId);
                if (!identity) {
                    return;
                }

                if (!this.currentMediaIdentity) {
                    this.currentMediaIdentity = identity;
                    this.currentItemId = context.itemId;
                } else if (hasReliableItemId && this.currentItemId && !areIdsEqual(this.currentItemId, context.itemId)) {
                    this.currentMediaIdentity = identity;
                    this.currentItemId = context.itemId;
                    this.requestSequence += 1;
                    this.setBusy(false);
                    this.setResult('');
                    this.setDebug('');
                    return;
                }

                if (this.isRecognitionDisplayStale(context)) {
                    this.setResult('');
                    this.setDebug('');
                }
            }

            isRecognitionDisplayStale(context) {
                if (this.lastStatusContext && this.lastStatusClearMode === SAME_SECOND_CLEAR_MODE && context) {
                    if (this.lastStatusContext.itemId && context.itemId && this.lastStatusContext.itemId !== context.itemId) {
                        return true;
                    }

                    var currentStatusTicks = Number(context.positionTicks);
                    var originalStatusTicks = Number(this.lastStatusContext.positionTicks);

                    return Number.isFinite(currentStatusTicks)
                        && Number.isFinite(originalStatusTicks)
                        && Math.floor(currentStatusTicks / TICKS_PER_SECOND) !== Math.floor(originalStatusTicks / TICKS_PER_SECOND);
                }

                if (!this.lastResponse || !this.lastResponseContext || !context) {
                    return false;
                }

                if (this.lastResponseContext.itemId && context.itemId && this.lastResponseContext.itemId !== context.itemId) {
                    return true;
                }

                var currentTicks = Number(context.positionTicks);
                var recognizedTicks = Number(this.lastResponseContext.positionTicks);

                return Number.isFinite(currentTicks)
                    && Number.isFinite(recognizedTicks)
                    && Math.abs(currentTicks - recognizedTicks) > TRACK_DISPLAY_MAX_DISTANCE_TICKS;
            }

            setBusy(isBusy) {
                this.isBusy = isBusy;

                if (this.button) {
                    this.button.disabled = isBusy;
                    this.button.classList.toggle('is-busy', isBusy);
                }
            }

            setResult(text, context, clearMode) {
                if (!text) {
                    this.lastStatusText = '';
                    this.lastResponse = null;
                    this.lastResponseContext = null;
                    this.lastStatusContext = null;
                    this.lastStatusClearMode = null;
                } else {
                    this.lastStatusText = text;
                    this.lastResponse = null;
                    this.lastResponseContext = null;
                    this.lastStatusContext = context && clearMode ? {
                        itemId: context.itemId,
                        mediaSourceId: context.mediaSourceId || '',
                        positionTicks: context.positionTicks
                    } : null;
                    this.lastStatusClearMode = context && clearMode ? clearMode : null;
                }

                if (!this.panel || !this.heading || !this.card || !this.title || !this.artist || !this.album || !this.actions) {
                    return;
                }

                if (!text) {
                    this.panel.classList.add('is-hidden');
                    this.copyText = '';
                    return;
                }

                var status = getFriendlyStatus(text, this.t);
                this.heading.textContent = this.t('recognition');
                this.card.className = 'auddRecognitionCard auddRecognitionStatus';
                this.panel.classList.remove('is-hidden');
                this.setCover('');
                this.title.textContent = status.title;
                this.artist.textContent = status.subtitle;
                this.album.textContent = '';
                this.actions.classList.add('is-hidden');
                this.copyText = status.subtitle ? status.title + ': ' + status.subtitle : status.title;
                this.panel.setAttribute('data-copy-text', this.copyText);
                if (this.copyButton) {
                    this.copyButton.setAttribute('data-copy-text', this.copyText);
                }
            }

            setRecognitionResponse(response, context) {
                if (getResponseStatus(response) !== 'recognized') {
                    var status = getResponseStatus(response);
                    this.setResult(
                        getText(response),
                        status === 'no_match' ? context : null,
                        status === 'no_match' ? SAME_SECOND_CLEAR_MODE : null);
                    return;
                }

                this.lastResponse = response;
                this.lastStatusText = '';
                this.lastStatusContext = null;
                this.lastStatusClearMode = null;
                if (context) {
                    this.lastResponseContext = {
                        itemId: context.itemId,
                        mediaSourceId: context.mediaSourceId || '',
                        positionTicks: context.positionTicks
                    };
                }

                if (!this.panel || !this.heading || !this.card || !this.title || !this.artist || !this.album || !this.actions) {
                    return;
                }

                var link = getPreferredMusicLink(response);
                var copyText = getTrackCopyText(response);

                this.heading.textContent = this.t('nowPlaying');
                this.card.className = 'auddRecognitionCard';
                this.panel.classList.remove('is-hidden');
                this.setCover(getAlbumArtUrl(response));
                this.title.textContent = getTrackTitle(response) || this.t('recognized');
                this.artist.textContent = getTrackArtist(response) || getTrackAlbum(response) || '';
                this.album.textContent = getTrackArtist(response) && getTrackAlbum(response) ? getTrackAlbum(response) : '';
                this.copyText = copyText;
                this.panel.setAttribute('data-copy-text', copyText);

                if (this.copyButton) {
                    this.copyButton.setAttribute('data-copy-text', copyText);
                }

                if (link && this.serviceLink && this.openLink) {
                    this.serviceLink.href = link.url;
                    this.serviceLink.textContent = link.label;
                    this.serviceLink.style.display = '';
                    this.openLink.href = link.url;
                    this.openLink.style.display = '';
                    if (this.copyButton) {
                        this.copyButton.style.borderLeft = '';
                    }

                    this.actions.classList.remove('is-hidden');
                } else {
                    if (this.serviceLink) {
                        this.serviceLink.removeAttribute('href');
                        this.serviceLink.style.display = 'none';
                    }

                    if (this.openLink) {
                        this.openLink.removeAttribute('href');
                        this.openLink.style.display = 'none';
                    }

                    if (this.copyButton) {
                        this.copyButton.style.borderLeft = '0';
                    }

                    this.actions.classList.remove('is-hidden');
                }
            }

            setCover(url) {
                if (!this.cover || !this.coverImage) {
                    return;
                }

                if (url) {
                    this.coverImage.src = url;
                    this.cover.classList.add('has-image');
                } else {
                    this.coverImage.removeAttribute('src');
                    this.cover.classList.remove('has-image');
                }
            }

            setDebug(text) {
                if (this.debug) {
                    this.debug.textContent = text || '';
                    if (text) {
                        this.debug.setAttribute('data-copy-text', text);
                    } else {
                        this.debug.removeAttribute('data-copy-text');
                    }
                }
            }

            stopOverlayEvent(event) {
                if (!event) {
                    return;
                }

                event.stopPropagation();
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

            handleCopyText(event) {
                this.consumeEvent(event);

                var target = event && event.currentTarget;
                var text = target && target.getAttribute ? target.getAttribute('data-copy-text') : '';
                text = text || this.copyText || (target && target.textContent ? target.textContent.trim() : '');
                if (!text) {
                    return;
                }

                copyTextToClipboard(text).then(function (copied) {
                    if (copied && target) {
                        var previousText = target.tagName === 'BUTTON' ? target.textContent : null;
                        target.title = this.t('copied');
                        if (previousText) {
                            target.textContent = this.t('copied');
                        }

                        window.setTimeout(function () {
                            if (target) {
                                target.title = this.t('copyHint');
                                if (previousText) {
                                    target.textContent = previousText;
                                }
                            }
                        }.bind(this), 1200);
                    }
                }.bind(this));
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

                context = await this.enrichPlaybackContext(context);

                if (!context.itemId) {
                    this.setResult('No item id');
                    return;
                }

                var identity = this.mediaIdentity(context);
                this.currentMediaIdentity = identity || this.currentMediaIdentity;
                this.currentItemId = context.itemId || this.currentItemId;

                var key = this.cacheKey(context);
                if (this.cache.has(key)) {
                    this.setRecognitionResponse(this.cache.get(key), context);
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
                        mediaSourcePath: context.mediaSourcePath,
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
                        this.setDebug(formatClipDebug(response, context, this.t));
                    }

                    this.setRecognitionResponse(response, context);
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
                globalize: window.Globalize || window.globalize,
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
