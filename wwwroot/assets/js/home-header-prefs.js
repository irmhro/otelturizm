(() => {
    if (window.__otHeaderPrefsInit) {
        return;
    }
    window.__otHeaderPrefsInit = true;

    const dropdowns = document.querySelectorAll('[data-nav-pref-dropdown]');
    if (dropdowns.length === 0) {
        return;
    }

    const closeAll = (except) => {
        dropdowns.forEach((item) => {
            if (item === except) {
                return;
            }
            item.classList.remove('is-open');
            const trigger = item.querySelector('[data-nav-pref-trigger]');
            const menu = item.querySelector('[data-nav-pref-menu]');
            trigger?.setAttribute('aria-expanded', 'false');
            menu?.setAttribute('hidden', '');
        });
    };

    dropdowns.forEach((item) => {
        const trigger = item.querySelector('[data-nav-pref-trigger]');
        const menu = item.querySelector('[data-nav-pref-menu]');

        if (!trigger || !menu) {
            return;
        }

        trigger.addEventListener('click', (event) => {
            event.stopPropagation();
            const willOpen = !item.classList.contains('is-open');
            closeAll(willOpen ? item : null);
            item.classList.toggle('is-open', willOpen);
            trigger.setAttribute('aria-expanded', willOpen ? 'true' : 'false');
            if (willOpen) {
                menu.removeAttribute('hidden');
            } else {
                menu.setAttribute('hidden', '');
            }
        });
    });

    document.addEventListener('click', () => closeAll(null));
    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape') {
            closeAll(null);
        }
    });
})();
