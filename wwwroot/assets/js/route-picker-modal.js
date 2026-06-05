(() => {
    const overlay = document.querySelector('[data-route-picker-overlay]');
    if (!overlay) return;

    if (overlay.parentElement !== document.body) {
        document.body.appendChild(overlay);
    }

    const sheet = overlay.querySelector('.route-modal-sheet');
    const openBtns = document.querySelectorAll('[data-route-picker-open]');
    const closeBtns = overlay.querySelectorAll('[data-route-picker-close]');
    const cards = overlay.querySelectorAll('.route-modal-card[data-route-label]');
    const track = document.querySelector('[data-route-marquee-track]');

    if (track && !track.querySelector('[data-marquee-clone]')) {
        Array.from(track.querySelectorAll('.route-marquee-chip:not([data-marquee-clone])')).forEach(chip => {
            const clone = chip.cloneNode(true);
            clone.setAttribute('data-marquee-clone', '1');
            clone.setAttribute('aria-hidden', 'true');
            track.appendChild(clone);
        });
    }

    let lastOpener = null;

    const open = (opener) => {
        const drawer = document.getElementById('homeDrawerMenu');
        const drawerOverlay = document.getElementById('homeDrawerOverlay');
        const menuToggle = document.getElementById('homeMenuToggle');
        if (drawer?.classList.contains('open')) {
            drawer.classList.remove('open');
            drawerOverlay?.classList.remove('open');
            menuToggle?.setAttribute('aria-expanded', 'false');
        }

        lastOpener = opener || null;
        overlay.removeAttribute('hidden');
        document.body.classList.add('route-modal-open');
        if (opener) {
            opener.setAttribute('aria-expanded', 'true');
        }
        closeBtns[0]?.focus();
    };

    const close = () => {
        overlay.setAttribute('hidden', '');
        document.body.classList.remove('route-modal-open');
        openBtns.forEach(btn => btn.setAttribute('aria-expanded', 'false'));
        (lastOpener || openBtns[0])?.focus();
        lastOpener = null;
    };

    openBtns.forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.preventDefault();
            open(btn);
        });
    });

    closeBtns.forEach(btn => btn.addEventListener('click', close));

    overlay.addEventListener('click', (e) => {
        if (e.target === overlay) close();
    });

    sheet?.addEventListener('click', (e) => e.stopPropagation());

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && !overlay.hasAttribute('hidden')) close();
    });

    cards.forEach(card => {
        card.addEventListener('click', () => close());
    });
})();
