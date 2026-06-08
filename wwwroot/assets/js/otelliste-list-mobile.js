(() => {
    'use strict';

    if (!window.matchMedia('(max-width: 900px)').matches) {
        return;
    }

    const grid = document.getElementById('otellisteHotelGrid');
    const mobileBar = document.getElementById('otellisteMobileBar');
    const filterCountEl = document.getElementById('otellisteActiveFilterCount');
    const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const galleryIntervalMs = 1500;

    /* Sticky bar — scroll-down compact mod */
    if (mobileBar) {
        let lastY = window.scrollY;
        let compact = false;

        const syncBar = () => {
            const y = window.scrollY;
            const nextCompact = y > 72 && y > lastY;
            if (nextCompact !== compact) {
                compact = nextCompact;
                mobileBar.classList.toggle('is-compact', compact);
            }
            lastY = y;
        };

        window.addEventListener('scroll', syncBar, { passive: true });
        syncBar();
    }

    /* Aktif filtre badge animasyonu */
    if (filterCountEl) {
        const observer = new MutationObserver(() => {
            filterCountEl.classList.remove('is-pulse');
            void filterCountEl.offsetWidth;
            if ((filterCountEl.getAttribute('data-count') || '0') !== '0') {
                filterCountEl.classList.add('is-pulse');
            }
        });
        observer.observe(filterCountEl, { attributes: true, attributeFilter: ['data-count'] });
    }

    /* Amenity +N genişlet */
    document.querySelectorAll('[data-amenity-more]').forEach((btn) => {
        btn.addEventListener('click', (event) => {
            event.preventDefault();
            event.stopPropagation();
            const wrap = btn.closest('.otelliste-card-amenities');
            if (wrap) {
                wrap.setAttribute('data-amenity-expanded', 'true');
            }
        });
    });

    /* Favori kalp pop + aria sync hook */
    document.addEventListener('click', (event) => {
        const btn = event.target.closest('.otelliste-fav-btn.favorite-btn');
        if (!btn) {
            return;
        }
        window.setTimeout(() => {
            if (btn.classList.contains('active') || btn.classList.contains('is-active')) {
                btn.classList.remove('is-fav-burst');
                void btn.offsetWidth;
                btn.classList.add('is-fav-burst');
                window.setTimeout(() => btn.classList.remove('is-fav-burst'), 520);
            }
        }, 140);
    }, true);

    /* CTA navigate loading state */
    grid?.querySelectorAll('.otelliste-card-cta').forEach((link) => {
        link.addEventListener('click', () => {
            link.classList.add('is-loading');
        });
    });

    if (!grid) {
        return;
    }

    const setGalleryImage = (img, url, animate) => {
        if (!animate || prefersReducedMotion) {
            img.src = url;
            return;
        }

        img.classList.add('is-gallery-fading');
        window.setTimeout(() => {
            img.src = url;
            img.classList.remove('is-gallery-fading');
        }, 180);
    };

    /* Kart galeri — otomatik geçiş + swipe */
    grid.querySelectorAll('.otelliste-hotel-card').forEach((card) => {
        const raw = card.getAttribute('data-gallery');
        if (!raw) {
            return;
        }

        let images;
        try {
            images = JSON.parse(raw);
        } catch {
            return;
        }

        if (!Array.isArray(images) || images.length < 2) {
            return;
        }

        const img = card.querySelector('.otelliste-card-media img');
        if (!img) {
            return;
        }

        let currentIdx = 0;
        let startX = 0;
        let tracking = false;
        let timerId = null;

        const advance = (step = 1, animate = true) => {
            currentIdx = (currentIdx + step + images.length) % images.length;
            setGalleryImage(img, images[currentIdx], animate);
        };

        const restartAuto = () => {
            if (prefersReducedMotion) {
                return;
            }
            if (timerId) {
                window.clearInterval(timerId);
            }
            timerId = window.setInterval(() => advance(1, true), galleryIntervalMs);
        };

        restartAuto();

        card.addEventListener('touchstart', (event) => {
            if (!event.touches || event.touches.length !== 1) {
                return;
            }
            startX = event.touches[0].clientX;
            tracking = true;
            if (timerId) {
                window.clearInterval(timerId);
                timerId = null;
            }
        }, { passive: true });

        card.addEventListener('touchend', (event) => {
            if (!tracking || !event.changedTouches || event.changedTouches.length !== 1) {
                tracking = false;
                restartAuto();
                return;
            }

            const deltaX = event.changedTouches[0].clientX - startX;
            tracking = false;
            if (Math.abs(deltaX) >= 36) {
                advance(deltaX < 0 ? 1 : -1, true);
            }
            restartAuto();
        }, { passive: true });

        card.addEventListener('touchcancel', () => {
            tracking = false;
            restartAuto();
        }, { passive: true });
    });
})();
