(function () {
    'use strict';

    var SEARCH_DATES_COOKIE = 'Otelturizm.SearchDates';
    var SEARCH_DATES_STORAGE = 'Otelturizm.SearchDates';

    function pad(value) {
        return String(value).padStart(2, '0');
    }

    function localToday() {
        var now = new Date();
        return now.getFullYear() + '-' + pad(now.getMonth() + 1) + '-' + pad(now.getDate());
    }

    function addDays(isoDate, days) {
        var parts = (isoDate || '').split('-').map(Number);
        if (parts.length !== 3 || parts.some(function (n) { return !Number.isFinite(n); })) {
            return localToday();
        }
        var date = new Date(parts[0], parts[1] - 1, parts[2]);
        date.setDate(date.getDate() + days);
        return date.getFullYear() + '-' + pad(date.getMonth() + 1) + '-' + pad(date.getDate());
    }

    function formatDateTr(iso) {
        if (!iso) {
            return '';
        }
        var parts = iso.split('-');
        if (parts.length !== 3) {
            return iso;
        }
        return parts[2] + '/' + parts[1] + '/' + parts[0];
    }

    function readCookie(name) {
        try {
            var parts = document.cookie.split(';').map(function (c) { return c.trim(); });
            var hit = parts.find(function (p) { return p.indexOf(name + '=') === 0; });
            if (!hit) {
                return '';
            }
            return decodeURIComponent(hit.substring(name.length + 1));
        } catch (_) {
            return '';
        }
    }

    function writeCookie(name, value, days) {
        try {
            var maxAge = Math.max(1, Math.floor(days * 24 * 60 * 60));
            var secure = window.location.protocol === 'https:' ? '; Secure' : '';
            document.cookie = name + '=' + encodeURIComponent(value) + '; Path=/; Max-Age=' + maxAge + '; SameSite=Lax' + secure;
        } catch (_) {
            // ignore
        }
    }

    function parseStoredPair(raw) {
        if (!raw) {
            return null;
        }
        var parts = String(raw).split('|');
        if (parts.length !== 2) {
            return null;
        }
        var checkIn = parts[0];
        var checkOut = parts[1];
        if (!/^\d{4}-\d{2}-\d{2}$/.test(checkIn) || !/^\d{4}-\d{2}-\d{2}$/.test(checkOut)) {
            return null;
        }
        return { checkIn: checkIn, checkOut: checkOut };
    }

    function normalizeStoredPair(checkIn, checkOut) {
        var today = localToday();
        if (!checkIn || checkIn < today) {
            checkIn = today;
        }
        if (!checkOut || checkOut <= checkIn) {
            checkOut = addDays(checkIn, 1);
        }
        return { checkIn: checkIn, checkOut: checkOut };
    }

    function persistSearchDates(checkIn, checkOut) {
        var normalized = normalizeStoredPair(checkIn, checkOut);
        var payload = normalized.checkIn + '|' + normalized.checkOut;
        writeCookie(SEARCH_DATES_COOKIE, payload, 30);
        try {
            sessionStorage.setItem(SEARCH_DATES_STORAGE, payload);
        } catch (_) {
            // ignore
        }
    }

    function readPersistedSearchDates() {
        var fromUrl = null;
        try {
            var params = new URLSearchParams(window.location.search);
            var urlCheckIn = params.get('checkIn') || '';
            var urlCheckOut = params.get('checkOut') || '';
            if (urlCheckIn) {
                fromUrl = normalizeStoredPair(urlCheckIn, urlCheckOut);
            }
        } catch (_) {
            fromUrl = null;
        }
        if (fromUrl) {
            return fromUrl;
        }

        var fromStorage = '';
        try {
            fromStorage = sessionStorage.getItem(SEARCH_DATES_STORAGE) || '';
        } catch (_) {
            fromStorage = '';
        }
        var parsedStorage = parseStoredPair(fromStorage);
        if (parsedStorage) {
            return normalizeStoredPair(parsedStorage.checkIn, parsedStorage.checkOut);
        }

        var parsedCookie = parseStoredPair(readCookie(SEARCH_DATES_COOKIE));
        if (parsedCookie) {
            return normalizeStoredPair(parsedCookie.checkIn, parsedCookie.checkOut);
        }

        return null;
    }

    function updateDateDisplay(input) {
        if (!(input instanceof HTMLInputElement)) {
            return;
        }
        var formatted = formatDateTr(input.value);
        if (formatted) {
            input.title = formatted;
            input.setAttribute('aria-label', formatted);
        }
        var hint = input.parentElement && input.parentElement.querySelector('[data-date-hint]');
        if (hint) {
            hint.textContent = formatted;
        }
    }

    function normalizePair(checkInEl, checkOutEl, options) {
        if (!(checkInEl instanceof HTMLInputElement) || !(checkOutEl instanceof HTMLInputElement)) {
            return;
        }

        var today = localToday();
        checkInEl.min = today;
        checkInEl.max = '';

        if (!checkInEl.value || checkInEl.value < today) {
            checkInEl.value = today;
        }

        var minCheckOut = addDays(checkInEl.value, 1);
        checkOutEl.min = minCheckOut;

        var checkInChanged = !!(options && options.checkInChanged);
        var checkoutTouched = checkOutEl.dataset.checkoutTouched === '1';

        if (checkInChanged && !checkoutTouched) {
            checkOutEl.value = minCheckOut;
        } else if (!checkOutEl.value || checkOutEl.value <= checkInEl.value) {
            checkOutEl.value = minCheckOut;
        }

        updateDateDisplay(checkInEl);
        updateDateDisplay(checkOutEl);
        persistSearchDates(checkInEl.value, checkOutEl.value);
    }

    function bindPair(checkInEl, checkOutEl) {
        if (!(checkInEl instanceof HTMLInputElement) || !(checkOutEl instanceof HTMLInputElement)) {
            return;
        }

        if (checkInEl.dataset.otBookingDatesBound === '1') {
            normalizePair(checkInEl, checkOutEl);
            return;
        }
        checkInEl.dataset.otBookingDatesBound = '1';
        checkOutEl.dataset.otBookingDatesBound = '1';

        normalizePair(checkInEl, checkOutEl);

        var syncFromCheckIn = function () {
            normalizePair(checkInEl, checkOutEl, { checkInChanged: true });
        };

        checkInEl.addEventListener('change', syncFromCheckIn);
        checkInEl.addEventListener('input', syncFromCheckIn);

        var syncFromCheckOut = function () {
            if (checkOutEl.value && checkOutEl.value > checkInEl.value) {
                checkOutEl.dataset.checkoutTouched = '1';
            }
            normalizePair(checkInEl, checkOutEl);
        };

        checkOutEl.addEventListener('change', syncFromCheckOut);
        checkOutEl.addEventListener('input', syncFromCheckOut);
    }

    function bindRoomItem(item) {
        if (!item) return;
        bindPair(
            item.querySelector('[data-field="checkIn"]'),
            item.querySelector('[data-field="checkOut"]')
        );
    }

    function applyPersistedDatesToHidden() {
        var stored = readPersistedSearchDates();
        if (!stored) {
            return null;
        }

        var hiddenCheckIn = document.getElementById('checkInDateInput');
        var hiddenCheckOut = document.getElementById('checkOutDateInput');
        if (hiddenCheckIn instanceof HTMLInputElement) {
            hiddenCheckIn.value = stored.checkIn;
        }
        if (hiddenCheckOut instanceof HTMLInputElement) {
            hiddenCheckOut.value = stored.checkOut;
        }
        return stored;
    }

    function initAll() {
        applyPersistedDatesToHidden();

        document.querySelectorAll('[data-room-item]').forEach(bindRoomItem);

        var hiddenCheckIn = document.getElementById('checkInDateInput');
        var hiddenCheckOut = document.getElementById('checkOutDateInput');
        if (hiddenCheckIn instanceof HTMLInputElement && hiddenCheckOut instanceof HTMLInputElement) {
            bindPair(hiddenCheckIn, hiddenCheckOut);
        }

        document.dispatchEvent(new CustomEvent('otelturizm:booking-dates-ready'));
    }

    window.otelturizmBookingDates = {
        localToday: localToday,
        addDays: addDays,
        formatDateTr: formatDateTr,
        normalizePair: normalizePair,
        bindRoomItem: bindRoomItem,
        persistSearchDates: persistSearchDates,
        readPersistedSearchDates: readPersistedSearchDates,
        applyPersistedDatesToHidden: applyPersistedDatesToHidden,
        initAll: initAll
    };

    document.addEventListener('otelturizm:room-item-added', function (event) {
        bindRoomItem(event.detail && event.detail.item);
    });
})();
