(function () {
    'use strict';

    function pad(value) {
        return String(value).padStart(2, '0');
    }

    function localToday() {
        const now = new Date();
        return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
    }

    function addDays(isoDate, days) {
        const parts = (isoDate || '').split('-').map(Number);
        if (parts.length !== 3 || parts.some(function (n) { return !Number.isFinite(n); })) {
            return localToday();
        }
        const date = new Date(parts[0], parts[1] - 1, parts[2]);
        date.setDate(date.getDate() + days);
        return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
    }

    function normalizePair(checkInEl, checkOutEl) {
        if (!(checkInEl instanceof HTMLInputElement) || !(checkOutEl instanceof HTMLInputElement)) {
            return;
        }

        const today = localToday();
        checkInEl.min = today;
        checkInEl.max = '';

        if (!checkInEl.value || checkInEl.value < today) {
            checkInEl.value = today;
        }

        const minCheckOut = addDays(checkInEl.value, 1);
        checkOutEl.min = minCheckOut;

        if (!checkOutEl.value || checkOutEl.value <= checkInEl.value) {
            checkOutEl.value = minCheckOut;
        }
    }

    function bindPair(checkInEl, checkOutEl) {
        if (!(checkInEl instanceof HTMLInputElement) || !(checkOutEl instanceof HTMLInputElement)) {
            return;
        }

        normalizePair(checkInEl, checkOutEl);

        const sync = function () {
            normalizePair(checkInEl, checkOutEl);
        };

        checkInEl.addEventListener('change', sync);
        checkInEl.addEventListener('input', sync);
        checkOutEl.addEventListener('change', function () {
            normalizePair(checkInEl, checkOutEl);
        });
        checkOutEl.addEventListener('input', function () {
            normalizePair(checkInEl, checkOutEl);
        });
    }

    function bindRoomItem(item) {
        if (!item) return;
        bindPair(
            item.querySelector('[data-field="checkIn"]'),
            item.querySelector('[data-field="checkOut"]')
        );
    }

    function initAll() {
        document.querySelectorAll('[data-room-item]').forEach(bindRoomItem);

        const hiddenCheckIn = document.getElementById('checkInDateInput');
        const hiddenCheckOut = document.getElementById('checkOutDateInput');
        if (hiddenCheckIn instanceof HTMLInputElement && hiddenCheckOut instanceof HTMLInputElement) {
            bindPair(hiddenCheckIn, hiddenCheckOut);
        }
    }

    window.otelturizmBookingDates = {
        localToday: localToday,
        addDays: addDays,
        normalizePair: normalizePair,
        bindRoomItem: bindRoomItem,
        initAll: initAll
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAll);
    } else {
        initAll();
    }

    document.addEventListener('otelturizm:room-item-added', function (event) {
        bindRoomItem(event.detail && event.detail.item);
    });
})();
