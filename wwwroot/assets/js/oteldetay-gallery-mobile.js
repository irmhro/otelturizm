(function () {
    'use strict';

    if (!window.matchMedia('(max-width: 900px)').matches) {
        return;
    }

    var strip = document.getElementById('galleryMobileStrip');
    var track = document.getElementById('galleryMobileTrack');
    if (!strip || !track) {
        return;
    }

    var images = (window.__otelDetayConfig && window.__otelDetayConfig.galleryImages) || [];
    var title = strip.getAttribute('data-gallery-title') || '';
    var activeIndex = 0;
    var scrollTicking = false;
    var tapStartX = 0;
    var tapStartY = 0;
    var tapMoved = false;
    var dragPointerId = null;
    var dragStartX = 0;
    var dragScrollLeft = 0;
    var isDragging = false;

    var dots = Array.from(document.querySelectorAll('[data-gallery-dot-index]'));
    var thumbs = Array.from(document.querySelectorAll('[data-gallery-mobile-thumb-index]'));

    function clampIndex(index) {
        if (!images.length) {
            return 0;
        }
        return Math.min(Math.max(index, 0), images.length - 1);
    }

    function updateChrome(index) {
        var safeIndex = clampIndex(index);
        activeIndex = safeIndex;

        var counter = document.getElementById('galleryMobileCounter');
        if (counter) {
            var current = counter.querySelector('.gallery-mobile-counter__current');
            if (current) {
                current.textContent = String(safeIndex + 1);
            }
        }

        dots.forEach(function (dot) {
            var dotIndex = parseInt(dot.getAttribute('data-gallery-dot-index') || '0', 10);
            var isActive = dotIndex === safeIndex;
            dot.classList.toggle('active', isActive);
            dot.setAttribute('aria-selected', isActive ? 'true' : 'false');
        });

        thumbs.forEach(function (thumb) {
            var thumbIndex = parseInt(thumb.getAttribute('data-gallery-mobile-thumb-index') || '0', 10);
            var isActive = thumbIndex === safeIndex;
            thumb.classList.toggle('active', isActive);
            if (isActive) {
                thumb.scrollIntoView({ behavior: 'smooth', inline: 'nearest', block: 'nearest' });
            }
        });

        if (typeof window.__otelDetayGallerySync === 'function') {
            window.__otelDetayGallerySync(safeIndex);
        }
    }

    function scrollToIndex(index, behavior) {
        var slide = track.querySelector('[data-gallery-index="' + clampIndex(index) + '"]');
        if (!slide) {
            return;
        }
        slide.scrollIntoView({
            behavior: behavior || 'smooth',
            inline: 'start',
            block: 'nearest'
        });
    }

    function openSlaytAt(index) {
        if (!window.SlaytGorsel || !images.length) {
            return;
        }
        var safeIndex = clampIndex(index);
        updateChrome(safeIndex);
        window.SlaytGorsel.open({
            images: images,
            title: title,
            startIndex: safeIndex
        });
    }

    function resolveIndexFromScroll() {
        var slides = Array.from(track.querySelectorAll('.gallery-mobile-slide'));
        if (!slides.length) {
            return activeIndex;
        }

        var trackRect = track.getBoundingClientRect();
        var trackCenter = trackRect.left + (trackRect.width / 2);
        var nearestIndex = activeIndex;
        var nearestDistance = Number.POSITIVE_INFINITY;

        slides.forEach(function (slide) {
            var rect = slide.getBoundingClientRect();
            var center = rect.left + (rect.width / 2);
            var distance = Math.abs(center - trackCenter);
            if (distance < nearestDistance) {
                nearestDistance = distance;
                nearestIndex = parseInt(slide.getAttribute('data-gallery-index') || '0', 10);
            }
        });

        return nearestIndex;
    }

    track.style.touchAction = 'pan-x';
    track.style.webkitOverflowScrolling = 'touch';

    track.addEventListener('scroll', function () {
        if (scrollTicking) {
            return;
        }
        scrollTicking = true;
        window.requestAnimationFrame(function () {
            var nearestIndex = resolveIndexFromScroll();
            if (nearestIndex !== activeIndex) {
                updateChrome(nearestIndex);
            }
            scrollTicking = false;
        });
    }, { passive: true });

    track.addEventListener('pointerdown', function (event) {
        if (event.pointerType === 'mouse' && event.button !== 0) {
            return;
        }
        tapStartX = event.clientX;
        tapStartY = event.clientY;
        tapMoved = false;
        isDragging = true;
        dragPointerId = event.pointerId;
        dragStartX = event.clientX;
        dragScrollLeft = track.scrollLeft;
        track.setPointerCapture(event.pointerId);
        track.classList.add('is-dragging');
    });

    track.addEventListener('pointermove', function (event) {
        if (!isDragging || event.pointerId !== dragPointerId) {
            return;
        }
        var dx = event.clientX - tapStartX;
        var dy = event.clientY - tapStartY;
        if (Math.abs(dx) > 8 || Math.abs(dy) > 8) {
            tapMoved = true;
        }
        if (Math.abs(dx) > Math.abs(dy)) {
            track.scrollLeft = dragScrollLeft - (event.clientX - dragStartX);
        }
    });

    function endPointerDrag(event) {
        if (!isDragging || (event && event.pointerId !== dragPointerId)) {
            return;
        }
        isDragging = false;
        track.classList.remove('is-dragging');
        try {
            if (event) {
                track.releasePointerCapture(event.pointerId);
            }
        } catch (_) { /* ignore */ }
        dragPointerId = null;
        window.requestAnimationFrame(function () {
            updateChrome(resolveIndexFromScroll());
        });
    }

    track.addEventListener('pointerup', endPointerDrag);
    track.addEventListener('pointercancel', endPointerDrag);

    track.querySelectorAll('.gallery-mobile-slide').forEach(function (slide) {
        slide.addEventListener('click', function (event) {
            if (tapMoved) {
                event.preventDefault();
                event.stopPropagation();
                return;
            }
            var index = parseInt(slide.getAttribute('data-gallery-index') || '0', 10);
            openSlaytAt(index);
        });
    });

    document.querySelectorAll('[data-gallery-mobile-nav]').forEach(function (button) {
        button.addEventListener('click', function (event) {
            event.preventDefault();
            event.stopPropagation();
            if (images.length < 2) {
                return;
            }
            var direction = button.getAttribute('data-gallery-mobile-nav');
            var delta = direction === 'prev' ? -1 : 1;
            var nextIndex = (activeIndex + delta + images.length) % images.length;
            updateChrome(nextIndex);
            scrollToIndex(nextIndex, 'smooth');
        });
    });

    dots.forEach(function (dot) {
        dot.addEventListener('click', function () {
            var index = parseInt(dot.getAttribute('data-gallery-dot-index') || '0', 10);
            updateChrome(index);
            scrollToIndex(index, 'smooth');
        });
    });

    thumbs.forEach(function (thumb) {
        thumb.addEventListener('click', function () {
            var index = parseInt(thumb.getAttribute('data-gallery-mobile-thumb-index') || '0', 10);
            updateChrome(index);
            scrollToIndex(index, 'smooth');
        });
    });

    document.querySelectorAll('.gallery-mobile-all-photos, [data-slayt-trigger="hotel-gallery"]').forEach(function (button) {
        if (!strip.contains(button) && button.closest('#galleryMobileStrip') === null) {
            return;
        }
        button.addEventListener('click', function (event) {
            event.preventDefault();
            var index = parseInt(button.getAttribute('data-gallery-index') || String(activeIndex), 10);
            openSlaytAt(index);
        });
    });

    window.__otelDetayMobileGallery = {
        syncIndex: updateChrome,
        scrollTo: scrollToIndex,
        openSlayt: openSlaytAt
    };

    updateChrome(0);
})();
