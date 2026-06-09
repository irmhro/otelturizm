(() => {
    const sections = document.querySelectorAll('[data-home-hotel-section]');
    if (!sections.length) return;

    sections.forEach((section) => {
        const track = section.querySelector('[data-hotel-scroll-track]');
        const prevBtn = section.querySelector('[data-hotel-scroll-prev]');
        const nextBtn = section.querySelector('[data-hotel-scroll-next]');
        if (!track) return;

        const cards = Array.from(track.querySelectorAll('.hotel-card'));
        if (cards.length === 0) return;

        const scrollStep = () => {
            const card = cards[0];
            if (!card) return track.clientWidth * 0.85;
            const styles = window.getComputedStyle(track.querySelector('.hotel-grid--scroll') || track);
            const gap = parseFloat(styles.columnGap || styles.gap || '16') || 16;
            return card.offsetWidth + gap;
        };

        const updateNav = () => {
            if (!prevBtn || !nextBtn) return;
            const maxScroll = track.scrollWidth - track.clientWidth;
            const hasOverflow = maxScroll > 8;
            const isDesktop = window.matchMedia('(min-width: 901px)').matches;
            prevBtn.hidden = !isDesktop || !hasOverflow;
            nextBtn.hidden = !isDesktop || !hasOverflow;
            if (!hasOverflow || !isDesktop) return;
            prevBtn.disabled = track.scrollLeft <= 4;
            nextBtn.disabled = track.scrollLeft >= maxScroll - 4;
        };

        prevBtn?.addEventListener('click', () => {
            track.scrollBy({ left: -scrollStep(), behavior: 'smooth' });
        });

        nextBtn?.addEventListener('click', () => {
            track.scrollBy({ left: scrollStep(), behavior: 'smooth' });
        });

        track.addEventListener('scroll', updateNav, { passive: true });
        window.addEventListener('resize', updateNav, { passive: true });
        updateNav();
    });
})();
