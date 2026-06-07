(function () {
    'use strict';

    if (!window.matchMedia('(max-width: 900px)').matches) return;

    var page = document.querySelector('.oteldetay-page');
    if (!page) return;

    /* Oda thumb → kapak onizleme */
    page.querySelectorAll('#roomsCard .room-card').forEach(function (card) {
        var coverImg = card.querySelector('.room-card-cover img');
        var thumbs = card.querySelectorAll('.room-thumb-item');
        if (!coverImg || !thumbs.length) return;

        thumbs.forEach(function (thumb) {
            thumb.setAttribute('type', 'button');
            thumb.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                var img = thumb.querySelector('img');
                if (!img || !img.src) return;
                coverImg.src = img.src;
                thumbs.forEach(function (t) {
                    t.classList.remove('active');
                    t.removeAttribute('aria-current');
                });
                thumb.classList.add('active');
                thumb.setAttribute('aria-current', 'true');
            });
        });
    });

    /* Rezervasyon sheet — swipe-down, focus trap */
    var bookingSidebar = document.getElementById('bookingSidebar');
    var bookingBackdrop = document.getElementById('bookingModalBackdrop');
    var focusTrapActive = false;
    var lastFocusedBeforeSheet = null;

    function getSheetFocusables() {
        if (!bookingSidebar) return [];
        return Array.from(bookingSidebar.querySelectorAll(
            'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'
        )).filter(function (el) {
            return el.offsetParent !== null || el === document.activeElement;
        });
    }

    function handleFocusTrap(event) {
        if (!focusTrapActive || event.key !== 'Tab' || !bookingSidebar) return;
        var items = getSheetFocusables();
        if (!items.length) return;
        var first = items[0];
        var last = items[items.length - 1];
        if (event.shiftKey && document.activeElement === first) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
            event.preventDefault();
            first.focus();
        }
    }

    function activateFocusTrap() {
        if (focusTrapActive || !bookingSidebar) return;
        focusTrapActive = true;
        lastFocusedBeforeSheet = document.activeElement;
        document.addEventListener('keydown', handleFocusTrap);
        window.setTimeout(function () {
            var items = getSheetFocusables();
            if (items.length) items[0].focus();
        }, 60);
    }

    function releaseFocusTrap() {
        if (!focusTrapActive) return;
        focusTrapActive = false;
        document.removeEventListener('keydown', handleFocusTrap);
        if (lastFocusedBeforeSheet && typeof lastFocusedBeforeSheet.focus === 'function') {
            try { lastFocusedBeforeSheet.focus(); } catch (_) { /* ignore */ }
        }
    }

    function closeSheetFromGesture() {
        var closeBtn = document.getElementById('bookingModalClose');
        if (closeBtn) {
            closeBtn.click();
            return;
        }
        if (bookingBackdrop) bookingBackdrop.click();
    }

    if (bookingSidebar) {
        var sheetStartY = 0;
        var sheetTracking = false;

        bookingSidebar.addEventListener('touchstart', function (event) {
            if (!bookingSidebar.classList.contains('is-open') || !event.touches || event.touches.length !== 1) {
                return;
            }
            var card = bookingSidebar.querySelector('.booking-card');
            if (card && card.scrollTop > 8) {
                return;
            }
            sheetStartY = event.touches[0].clientY;
            sheetTracking = true;
        }, { passive: true });

        bookingSidebar.addEventListener('touchmove', function (event) {
            if (!sheetTracking || !event.touches || event.touches.length !== 1) {
                return;
            }
            if (event.touches[0].clientY - sheetStartY > 88) {
                sheetTracking = false;
                closeSheetFromGesture();
            }
        }, { passive: true });

        bookingSidebar.addEventListener('touchend', function () {
            sheetTracking = false;
        }, { passive: true });

        function syncSheetBodyClass() {
            var open = bookingSidebar.classList.contains('is-open');
            document.body.classList.toggle('booking-sheet-open', open);
            var shell = document.querySelector('.oteldetay-page-shell');
            if (shell) {
                shell.classList.toggle('booking-sheet-open', open);
            }
        }

        var sheetObserver = new MutationObserver(function () {
            syncSheetBodyClass();
            if (bookingSidebar.classList.contains('is-open')) {
                activateFocusTrap();
            } else {
                releaseFocusTrap();
            }
        });
        sheetObserver.observe(bookingSidebar, { attributes: true, attributeFilter: ['class'] });
        syncSheetBodyClass();
    }

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape' && bookingSidebar && bookingSidebar.classList.contains('is-open')) {
            closeSheetFromGesture();
        }
    });
})();
