(() => {
    const root = document.querySelector('[data-category-marquee]');
    const track = root?.querySelector('.category-marquee-track');
    if (!root || !track) return;

    const originals = Array.from(track.querySelectorAll('.cat-item:not([data-marquee-clone])'));
    if (originals.length === 0) return;

    if (!track.querySelector('[data-marquee-clone]')) {
        originals.forEach(item => {
            const clone = item.cloneNode(true);
            clone.setAttribute('data-marquee-clone', '1');
            clone.setAttribute('aria-hidden', 'true');
            clone.setAttribute('tabindex', '-1');
            track.appendChild(clone);
        });
    }
})();
