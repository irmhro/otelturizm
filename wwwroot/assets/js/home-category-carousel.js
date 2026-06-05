(() => {
    const shell = document.querySelector('[data-category-carousel]');
    if (!shell) return;

    const track = shell.querySelector('.category-carousel');
    const items = Array.from(track?.querySelectorAll('.cat-item') || []);
    const prevBtn = shell.querySelector('[data-cat-prev]');
    const nextBtn = shell.querySelector('[data-cat-next]');
    if (!track || items.length === 0) return;

    let activeIndex = items.findIndex(el => el.classList.contains('active'));
    if (activeIndex < 0) activeIndex = 0;

    const setActive = (index, scroll = true) => {
        activeIndex = (index + items.length) % items.length;
        items.forEach((item, i) => {
            const isActive = i === activeIndex;
            item.classList.toggle('active', isActive);
            item.setAttribute('aria-selected', isActive ? 'true' : 'false');
        });
        if (scroll) {
            items[activeIndex].scrollIntoView({ behavior: 'smooth', inline: 'center', block: 'nearest' });
        }
        prevBtn?.toggleAttribute('disabled', activeIndex === 0);
        nextBtn?.toggleAttribute('disabled', activeIndex === items.length - 1);
    };

    prevBtn?.addEventListener('click', () => setActive(activeIndex - 1));
    nextBtn?.addEventListener('click', () => setActive(activeIndex + 1));

    items.forEach((item, index) => {
        item.addEventListener('click', (e) => {
            if (e.metaKey || e.ctrlKey || e.shiftKey || e.altKey || e.button !== 0) return;
            setActive(index);
        });
    });

    let scrollTimer;
    track.addEventListener('scroll', () => {
        window.clearTimeout(scrollTimer);
        scrollTimer = window.setTimeout(() => {
            const center = track.scrollLeft + track.clientWidth / 2;
            let nearest = activeIndex;
            let nearestDist = Infinity;
            items.forEach((item, i) => {
                const itemCenter = item.offsetLeft + item.offsetWidth / 2;
                const dist = Math.abs(center - itemCenter);
                if (dist < nearestDist) {
                    nearestDist = dist;
                    nearest = i;
                }
            });
            if (nearest !== activeIndex) setActive(nearest, false);
        }, 80);
    }, { passive: true });

    setActive(activeIndex, false);
})();
