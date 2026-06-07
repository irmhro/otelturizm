(function () {
    'use strict';

    if (!window.matchMedia('(max-width: 900px)').matches) return;

    var page = document.querySelector('.oteldetay-page');
    if (!page) return;

    /* Galeri — parmakla kaydirma (touch pan + scroll snap) */
    var track = document.getElementById('galleryMobileTrack');
    if (track) {
        track.style.touchAction = 'pan-x';
        track.style.webkitOverflowScrolling = 'touch';

        var isDragging = false;
        var startX = 0;
        var scrollStart = 0;

        track.addEventListener('pointerdown', function (e) {
            if (e.pointerType === 'mouse' && e.button !== 0) return;
            isDragging = true;
            startX = e.clientX;
            scrollStart = track.scrollLeft;
            track.setPointerCapture(e.pointerId);
            track.classList.add('is-dragging');
        });

        track.addEventListener('pointermove', function (e) {
            if (!isDragging) return;
            track.scrollLeft = scrollStart - (e.clientX - startX);
        });

        function endDrag(e) {
            if (!isDragging) return;
            isDragging = false;
            track.classList.remove('is-dragging');
            try { track.releasePointerCapture(e.pointerId); } catch (_) { /* ignore */ }
        }

        track.addEventListener('pointerup', endDrag);
        track.addEventListener('pointercancel', endDrag);
    }

    /* Oda thumb → kapak onizleme */
    page.querySelectorAll('#roomsCard .room-card').forEach(function (card) {
        var coverImg = card.querySelector('.room-card-cover img');
        var thumbs = card.querySelectorAll('.room-thumb-item');
        if (!coverImg || !thumbs.length) return;

        thumbs.forEach(function (thumb) {
            thumb.addEventListener('click', function (e) {
                if (e.defaultPrevented) return;
                var img = thumb.querySelector('img');
                if (!img || !img.src) return;
                coverImg.src = img.src;
                thumbs.forEach(function (t) { t.classList.remove('active'); });
                thumb.classList.add('active');
            });
        });
    });
})();
