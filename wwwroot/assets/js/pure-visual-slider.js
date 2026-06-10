(function () {
    'use strict';

    if (window.PureVisualSlider) {
        return;
    }

    var instances = new Map();
    var modalInstance = null;
    var modalRoot = null;
    var modalWrapper = null;
    var modalMeta = null;
    var modalTitle = '';
    var modalImages = [];
    var lastFocus = null;
    var prevScrollY = 0;
    var modalOpening = false;

    function uniqueImages(images) {
        var seen = new Set();
        return images.filter(function (src) {
            if (!src || seen.has(src)) {
                return false;
            }
            seen.add(src);
            return true;
        });
    }

    function buildOptions(root) {
        var isModal = root.getAttribute('data-pure-visual-modal') === 'true';
        var slideCount = root.querySelectorAll('.swiper-slide').length;
        var opts = {
            effect: 'slide',
            speed: isModal ? 320 : 800,
            loop: !isModal && slideCount > 2,
            grabCursor: true,
            keyboard: { enabled: true },
            watchOverflow: true,
            resistanceRatio: 0.82
        };

        if (isModal) {
            opts.spaceBetween = 0;
            opts.centeredSlides = true;
        } else if (slideCount > 1) {
            opts.autoplay = {
                delay: 4000,
                disableOnInteraction: false
            };
        }

        if (slideCount > 1) {
            var paginationEl = root.querySelector('.swiper-pagination');
            if (paginationEl) {
                opts.pagination = {
                    el: paginationEl,
                    clickable: true,
                    dynamicBullets: !isModal
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

    function updateModalMeta(swiper) {
        if (!modalMeta || !modalImages.length) {
            return;
        }
        var index = typeof swiper?.realIndex === 'number'
            ? swiper.realIndex
            : (typeof swiper?.activeIndex === 'number' ? swiper.activeIndex : 0);
        index = Math.min(Math.max(index, 0), modalImages.length - 1);
        var counter = modalMeta.querySelector('[data-pvs-counter]');
        if (counter) {
            counter.textContent = (index + 1) + ' / ' + modalImages.length;
        }
        var titleEl = modalMeta.querySelector('[data-pvs-title]');
        if (titleEl) {
            titleEl.textContent = modalTitle || '';
            titleEl.hidden = !modalTitle;
        }
    }

    function initElement(root) {
        if (!(root instanceof HTMLElement) || root.dataset.pvsInitialized === 'true') {
            return instances.get(root.id) || null;
        }
        if (typeof window.Swiper !== 'function') {
            return null;
        }

        var isModal = root.getAttribute('data-pure-visual-modal') === 'true';
        var options = buildOptions(root);
        var swiper = new window.Swiper(root, options);
        root.dataset.pvsInitialized = 'true';
        if (root.id) {
            instances.set(root.id, swiper);
        }

        swiper.on('slideChange', function () {
            var realIndex = typeof swiper.realIndex === 'number' ? swiper.realIndex : swiper.activeIndex;
            if (isModal) {
                updateModalMeta(swiper);
            }
            document.dispatchEvent(new CustomEvent('pure-visual-slider:change', {
                detail: { id: root.id, index: realIndex }
            }));
        });

        if (isModal) {
            updateModalMeta(swiper);
        }

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
        modalMeta = document.getElementById('pureVisualSliderModalMeta');
        var modalSwiperEl = document.getElementById('pureVisualSliderModalSwiper');
        if (!modalRoot || !modalWrapper || !modalSwiperEl) {
            return false;
        }
        return true;
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
        if (modalOpening) {
            return;
        }

        var images = uniqueImages(Array.isArray(payload?.images) ? payload.images.filter(Boolean) : []);
        if (!images.length || !ensureModalDom()) {
            return;
        }

        modalOpening = true;
        modalImages = images;
        modalTitle = String(payload?.title || '').trim();

        var startIndex = parseInt(payload?.startIndex || '0', 10) || 0;
        startIndex = Math.min(Math.max(startIndex, 0), images.length - 1);

        if (modalInstance && typeof modalInstance.destroy === 'function') {
            modalInstance.destroy(true, true);
            modalInstance = null;
            var oldSwiperEl = document.getElementById('pureVisualSliderModalSwiper');
            if (oldSwiperEl) {
                oldSwiperEl.dataset.pvsInitialized = 'false';
            }
        }

        renderModalSlides(images);
        modalInstance = initElement(document.getElementById('pureVisualSliderModalSwiper'));
        if (!modalInstance) {
            modalOpening = false;
            return;
        }

        lastFocus = document.activeElement;
        prevScrollY = window.scrollY || 0;
        modalRoot.hidden = false;
        modalRoot.setAttribute('aria-hidden', 'false');
        document.body.classList.add('pure-visual-slider-open');
        updateModalMeta(modalInstance);

        if (typeof modalInstance.slideTo === 'function') {
            modalInstance.slideTo(startIndex, 0);
        }

        var closeBtn = modalRoot.querySelector('[data-pvs-close]');
        closeBtn?.focus?.();
        window.setTimeout(function () {
            modalOpening = false;
        }, 0);
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
