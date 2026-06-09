(() => {
    if (window.__otDrawerPrefsInit) {
        return;
    }
    window.__otDrawerPrefsInit = true;

    const accordions = document.querySelectorAll('[data-pref-accordion]');
    if (accordions.length === 0) {
        return;
    }

    const closeAll = (except) => {
        accordions.forEach((item) => {
            if (item === except) {
                return;
            }
            item.classList.remove('is-open');
            const trigger = item.querySelector('[data-pref-trigger]');
            const panel = item.querySelector('[data-pref-panel]');
            trigger?.setAttribute('aria-expanded', 'false');
            panel?.setAttribute('hidden', '');
        });
    };

    accordions.forEach((item) => {
        const trigger = item.querySelector('[data-pref-trigger]');
        const panel = item.querySelector('[data-pref-panel]');

        if (!trigger || !panel) {
            return;
        }

        trigger.addEventListener('click', () => {
            const willOpen = !item.classList.contains('is-open');
            closeAll(willOpen ? item : null);
            item.classList.toggle('is-open', willOpen);
            trigger.setAttribute('aria-expanded', willOpen ? 'true' : 'false');
            if (willOpen) {
                panel.removeAttribute('hidden');
            } else {
                panel.setAttribute('hidden', '');
            }
        });
    });
})();
