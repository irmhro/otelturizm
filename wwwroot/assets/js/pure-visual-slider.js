(function () {
    'use strict';

    if (window.PureVisualSlider) {
        return;
    }

    var instances = new Map();
    var modalInstance = null;
    var modalRoot = null;
    var modalWrapper = null;
    var lastFocus = null;
    var prevScrollY = 0;

    function buildOptions(root) {
        var slideCount = root.querySelectorAll('.swiper-slide').length;
        var opts = {
            effect: 'slide',
            speed: 800,
            loop: slideCount > 1,
            grabCursor: true,
            keyboard: { enabled: true }
        };

        if (slideCount > 1) {
            opts.autoplay = {
                delay: 4000,
                disableOnInteraction: false
            };
            var paginationEl = root.querySelector('.swiper-pagination');
            if (paginationEl) {
                opts.pagination = {
                    el: paginationEl,
                    clickable: true,
                    dynamicBullets: true
                };
            }
            var nextEl = root.querySelector('.swiper-button-next');
            var prevEl = root.querySelector('.swiper-button-prev');
            if (nextEl && prevEl) {
                opts.navigation = { nextEl: nextEl, prevEl: prevEl };
            }
        }

        return opts;
    }

    function initElement(root) {
        if (!(root instanceof HTMLElement) || root.dataset.pvsInitialized === 'true') {
            return instances.get(root.id) || null;
        }
        if (typeof window.Swiper !== 'function') {
            return null;
        }

        var options = buildOptions(root);
        var swiper = new window.Swiper(root, options);
        root.dataset.pvsInitialized = 'true';
        if (root.id) {
            instances.set(root.id, swiper);
        }

        swiper.on('slideChange', function () {
            var realIndex = typeof swiper.realIndex === 'number' ? swiper.realIndex : swiper.activeIndex;
            document.dispatchEvent(new CustomEvent('pure-visual-slider:change', {
                detail: { id: root.id, index: realIndex }
            }));
        });

        return swiper;
    }

    function initAll() {
        document.querySelectorAll('[data-pure-visual-slider]').forEach(function (el) {
            if (el.getAttribute('data-pure-visual-modal') === 'true') {
                return;
            }
            initElement(el);
        });
    }

    function ensureModalDom() {
        modalRoot = document.getElementById('pureVisualSliderModal');
        modalWrapper = document.getElementById('pureVisualSliderModalWrapper');
        var modalSwiperEl = document.getElementById('pureVisualSliderModalSwiper');
        if (!modalRoot || !modalWrapper || !modalSwiperEl) {
            return false;
        }
        if (!modalInstance && modalSwiperEl.dataset.pvsInitialized !== 'true') {
            modalInstance = initElement(modalSwiperEl);
        }
        return !!modalInstance;
    }

    function renderModalSlides(images) {
        if (!modalWrapper) {
            return;
        }
        modalWrapper.innerHTML = images.map(function (src, index) {
            var loading = index === 0 ? 'eager' : 'lazy';
            var fetch = index === 0 ? ' fetchpriority="high"' : '';
            return '<div class="swiper-slide">' +
                '<img src="' + src + '" alt="Görsel ' + (index + 1) + '" loading="' + loading + '"' + fetch + ' decoding="async" />' +
                '</div>';
        }).join('');
    }

    function openModal(payload) {
        var images = Array.isArray(payload?.images) ? payload.images.filter(Boolean) : [];
        if (!images.length || !ensureModalDom()) {
            return;
        }

        var startIndex = parseInt(payload?.startIndex || '0', 10) || 0;
        startIndex = Math.min(Math.max(startIndex, 0), images.length - 1);

        if (modalInstance && typeof modalInstance.destroy === 'function') {
            modalInstance.destroy(true, true);
            modalInstance = null;
            var modalSwiperEl = document.getElementById('pureVisualSliderModalSwiper');
            if (modalSwiperEl) {
                modalSwiperEl.dataset.pvsInitialized = 'false';
            }
        }

        renderModalSlides(images);
        modalInstance = initElement(document.getElementById('pureVisualSliderModalSwiper'));
        if (!modalInstance) {
            return;
        }

        lastFocus = document.activeElement;
        prevScrollY = window.scrollY || 0;
        modalRoot.hidden = false;
        modalRoot.setAttribute('aria-hidden', 'false');
        document.body.classList.add('pure-visual-slider-open');

        if (images.length > 1 && typeof modalInstance.slideToLoop === 'function') {
            modalInstance.slideToLoop(startIndex, 0);
        } else if (typeof modalInstance.slideTo === 'function') {
            modalInstance.slideTo(startIndex, 0);
        }

        var closeBtn = modalRoot.querySelector('[data-pvs-close]');
        closeBtn?.focus?.();
    }

    function closeModal() {
        if (!modalRoot) {
            return;
        }
        modalRoot.hidden = true;
        modalRoot.setAttribute('aria-hidden', 'true');
        document.body.classList.remove('pure-visual-slider-open');
        window.scrollTo(0, prevScrollY || 0);
        if (lastFocus && typeof lastFocus.focus === 'function') {
            lastFocus.focus();
        }
    }

    function open(payload) {
        openModal(payload);
    }

    function getInstance(id) {
        return instances.get(id) || null;
    }

    document.addEventListener('click', function (event) {
        var target = event.target instanceof HTMLElement ? event.target : null;
        if (!target) {
            return;
        }
        if (target.closest('[data-pvs-close]')) {
            event.preventDefault();
            closeModal();
        }
    });

    document.addEventListener('keydown', function (event) {
        if (!modalRoot || modalRoot.hidden) {
            return;
        }
        if (event.key === 'Escape') {
            event.preventDefault();
            closeModal();
        }
    });

    function boot() {
        initAll();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', boot);
    } else {
        boot();
    }

    window.PureVisualSlider = {
        initAll: initAll,
        init: initElement,
        getInstance: getInstance,
        open: open,
        close: closeModal
    };
})();
