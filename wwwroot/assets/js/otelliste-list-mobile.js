(() => {
    if (!window.matchMedia('(max-width: 900px)').matches) {
        return;
    }

    const grid = document.getElementById('otellisteHotelGrid');
    if (!grid) {
        return;
    }

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

        card.addEventListener('touchstart', (event) => {
            if (!event.touches || event.touches.length !== 1) {
                return;
            }
            startX = event.touches[0].clientX;
            tracking = true;
        }, { passive: true });

        card.addEventListener('touchend', (event) => {
            if (!tracking || !event.changedTouches || event.changedTouches.length !== 1) {
                tracking = false;
                return;
            }

            const deltaX = event.changedTouches[0].clientX - startX;
            tracking = false;
            if (Math.abs(deltaX) < 36) {
                return;
            }

            currentIdx = deltaX < 0
                ? (currentIdx + 1) % images.length
                : (currentIdx - 1 + images.length) % images.length;
            img.src = images[currentIdx];
        }, { passive: true });

        card.addEventListener('touchcancel', () => {
            tracking = false;
        }, { passive: true });
    });
})();
