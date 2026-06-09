(function () {
    'use strict';

    var STORAGE_KEY = 'otelturizm.popupMesaj.lastShownDate';
    var popup = document.getElementById('popupMesaj');
    if (!popup) {
        return;
    }

    function todayKey() {
        var d = new Date();
        var y = d.getFullYear();
        var m = String(d.getMonth() + 1).padStart(2, '0');
        var day = String(d.getDate()).padStart(2, '0');
        return y + '-' + m + '-' + day;
    }

    function wasShownToday() {
        try {
            return localStorage.getItem(STORAGE_KEY) === todayKey();
        } catch {
            return false;
        }
    }

    function markShownToday() {
        try {
            localStorage.setItem(STORAGE_KEY, todayKey());
        } catch {
            // ignore
        }
    }

    function openPopup() {
        popup.hidden = false;
        popup.setAttribute('aria-hidden', 'false');
        popup.classList.add('is-open');
        document.body.classList.add('popupmesaj-open');
        var dismissBtn = popup.querySelector('.popupmesaj__dismiss');
        if (dismissBtn) {
            dismissBtn.focus();
        }
    }

    function closePopup(persist) {
        popup.hidden = true;
        popup.setAttribute('aria-hidden', 'true');
        popup.classList.remove('is-open');
        document.body.classList.remove('popupmesaj-open');
        if (persist !== false) {
            markShownToday();
        }
    }

    if (wasShownToday()) {
        return;
    }

    popup.querySelectorAll('[data-popupmesaj-close]').forEach(function (el) {
        el.addEventListener('click', function () {
            closePopup(true);
        });
    });

    popup.querySelectorAll('[data-popupmesaj-register]').forEach(function (el) {
        el.addEventListener('click', function () {
            markShownToday();
        });
    });

    document.addEventListener('keydown', function (evt) {
        if (evt.key === 'Escape' && !popup.hidden) {
            closePopup(true);
        }
    });

    window.setTimeout(openPopup, 700);
})();
