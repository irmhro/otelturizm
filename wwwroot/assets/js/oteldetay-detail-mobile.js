(function () {
    'use strict';

    if (!window.matchMedia('(max-width: 900px)').matches) return;

    var page = document.querySelector('.oteldetay-page');
    if (!page) return;

    function initSwipeCarousel(track, dots, onIndexChange, autoMs) {
        if (!track) return function () { };

        var slides = Array.from(track.querySelectorAll('.room-carousel-slide, .room-detail-gallery-slide'));
        if (slides.length <= 1) return function () { };

        var index = 0;
        var timer = null;
        var startX = 0;
        var tracking = false;

        function setIndex(next) {
            index = (next + slides.length) % slides.length;
            track.scrollTo({ left: index * track.clientWidth, behavior: 'smooth' });
            if (dots) {
                dots.querySelectorAll('.room-carousel-dot, .room-detail-gallery-dot').forEach(function (dot, dotIndex) {
                    dot.classList.toggle('active', dotIndex === index);
                });
            }
            if (typeof onIndexChange === 'function') {
                onIndexChange(index);
            }
        }

        function startAuto() {
            stopAuto();
            if (!autoMs) return;
            timer = window.setInterval(function () {
                setIndex(index + 1);
            }, autoMs);
        }

        function stopAuto() {
            if (timer) {
                window.clearInterval(timer);
                timer = null;
            }
        }

        track.addEventListener('scroll', function () {
            if (!track.clientWidth) return;
            var next = Math.round(track.scrollLeft / track.clientWidth);
            if (next !== index) {
                index = next;
                if (dots) {
                    dots.querySelectorAll('.room-carousel-dot, .room-detail-gallery-dot').forEach(function (dot, dotIndex) {
                        dot.classList.toggle('active', dotIndex === index);
                    });
                }
            }
        }, { passive: true });

        track.addEventListener('touchstart', function (event) {
            if (!event.touches || event.touches.length !== 1) return;
            startX = event.touches[0].clientX;
            tracking = true;
            stopAuto();
        }, { passive: true });

        track.addEventListener('touchend', function () {
            tracking = false;
            startAuto();
        }, { passive: true });

        if (dots) {
            dots.querySelectorAll('.room-carousel-dot, .room-detail-gallery-dot').forEach(function (dot) {
                dot.addEventListener('click', function () {
                    var target = parseInt(dot.getAttribute('data-room-dot') || dot.getAttribute('data-gallery-dot') || '0', 10) || 0;
                    setIndex(target);
                    startAuto();
                });
            });
        }

        startAuto();
        return stopAuto;
    }

    page.querySelectorAll('[data-room-carousel]').forEach(function (carousel) {
        var track = carousel.querySelector('.room-carousel-track');
        var dots = carousel.querySelector('.room-carousel-dots');
        initSwipeCarousel(track, dots, null, 2000);

        carousel.addEventListener('click', function (event) {
            if (event.target.closest('.room-carousel-dot')) return;
            if (event.target.closest('.room-gallery-trigger')) return;
            var trigger = carousel.querySelector('.room-gallery-trigger');
            if (!trigger) return;
            var startIdx = 0;
            if (track && track.clientWidth > 0) {
                startIdx = Math.round(track.scrollLeft / track.clientWidth);
            }
            trigger.setAttribute('data-room-gallery-start', String(startIdx));
            trigger.click();
        });
    });

    document.addEventListener('otelturizm:room-detail-gallery-ready', function (event) {
        var track = event.detail?.track;
        var dotsRoot = event.detail?.dotsRoot;
        initSwipeCarousel(track, dotsRoot, null, 0);
    });

    /* Rezervasyon sheet — swipe-down, focus trap */
    var bookingSidebar = document.getElementById('rezervasyonAksiyon') || document.getElementById('bookingSidebar');
    var bookingBackdrop = document.getElementById('rezervasyonAksiyonBackdrop') || document.getElementById('bookingModalBackdrop');
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

    function resetBookingSheetScroll() {
        if (!bookingSidebar) return;
        var formEl = bookingSidebar.querySelector('.booking-form');
        if (formEl) formEl.scrollTop = 0;
        var cardEl = bookingSidebar.querySelector('.booking-card');
        if (cardEl) cardEl.scrollTop = 0;
    }

    function getSheetScrollEl() {
        if (!bookingSidebar) return null;
        return bookingSidebar.querySelector('.booking-form') || bookingSidebar.querySelector('.booking-card');
    }

    if (bookingSidebar) {
        var sheetStartY = 0;
        var sheetTracking = false;

        bookingSidebar.addEventListener('touchstart', function (event) {
            if (!bookingSidebar.classList.contains('is-open') || !event.touches || event.touches.length !== 1) {
                return;
            }
            var scrollEl = getSheetScrollEl();
            if (scrollEl && scrollEl.scrollTop > 8) {
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
                resetBookingSheetScroll();
                requestAnimationFrame(resetBookingSheetScroll);
                if (!document.querySelector('#detailActionBackdrop.is-open')) {
                    activateFocusTrap();
                } else {
                    releaseFocusTrap();
                }
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
