(() => {
    'use strict';

    const checkIn = document.getElementById('home-checkin');
    const checkOut = document.getElementById('home-checkout');
    if (!(checkIn instanceof HTMLInputElement) || !(checkOut instanceof HTMLInputElement)) {
        return;
    }

    const pad = (value) => String(value).padStart(2, '0');

    const localToday = () => {
        const now = new Date();
        return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
    };

    const addDays = (isoDate, days) => {
        const parts = (isoDate || '').split('-').map(Number);
        if (parts.length !== 3 || parts.some((n) => !Number.isFinite(n))) {
            return localToday();
        }
        const date = new Date(parts[0], parts[1] - 1, parts[2]);
        date.setDate(date.getDate() + days);
        return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
    };

    const syncDates = () => {
        const today = localToday();
        checkIn.min = today;
        checkIn.setAttribute('min', today);

        if (!checkIn.value || checkIn.value < today) {
            checkIn.value = today;
        }

        const minCheckOut = addDays(checkIn.value, 1);
        checkOut.min = minCheckOut;
        checkOut.setAttribute('min', minCheckOut);

        if (!checkOut.value || checkOut.value <= checkIn.value) {
            checkOut.value = addDays(checkIn.value, 7);
        }
    };

    const refreshBeforePicker = () => {
        syncDates();
    };

    const bindPrePicker = (input) => {
        ['focus', 'click', 'pointerdown', 'touchstart'].forEach((eventName) => {
            input.addEventListener(eventName, refreshBeforePicker, { capture: true });
        });

        const wrapper = input.closest('.search-pro-input');
        if (wrapper) {
            wrapper.addEventListener('click', (event) => {
                if (event.target === input) {
                    return;
                }
                refreshBeforePicker();
                input.focus();
                if (typeof input.showPicker === 'function') {
                    try {
                        input.showPicker();
                    } catch {
                        // showPicker requires a direct user gesture; ignore if blocked
                    }
                }
            });
        }
    };

    checkIn.addEventListener('change', syncDates);
    checkIn.addEventListener('input', syncDates);
    checkOut.addEventListener('change', () => {
        if (checkOut.value <= checkIn.value) {
            checkOut.value = addDays(checkIn.value, 1);
        }
    });
    checkOut.addEventListener('input', () => {
        if (checkOut.value <= checkIn.value) {
            checkOut.value = addDays(checkIn.value, 1);
        }
    });

    document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'visible') {
            syncDates();
        }
    });

    bindPrePicker(checkIn);
    bindPrePicker(checkOut);
    syncDates();
})();
