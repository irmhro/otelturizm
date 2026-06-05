(() => {
    if (window.__otHeaderPrefsInit) {
        return;
    }
    window.__otHeaderPrefsInit = true;

    const dropdowns = document.querySelectorAll('[data-nav-pref-dropdown]');
    if (dropdowns.length === 0) {
        return;
    }

    const shell = document.querySelector('.home-header-shell');
    const savedCurrency = (shell?.getAttribute('data-current-currency') || 'TRY').toUpperCase();

    const buildReturnUrl = () => `${window.location.pathname}${window.location.search}`;

    const redirectCurrency = (code) => {
        const normalized = (code || 'TRY').toUpperCase();
        const returnUrl = encodeURIComponent(buildReturnUrl());
        window.location.href = `/currency/set?code=${encodeURIComponent(normalized)}&returnUrl=${returnUrl}`;
    };

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
        const valueEl = item.querySelector('[data-pref-value]');
        const options = item.querySelectorAll('.nav-pref-option');
        const isCurrency = trigger?.classList.contains('nav-pref-icon-btn--currency');

        if (!trigger || !menu) {
            return;
        }

        if (isCurrency) {
            options.forEach((option) => {
                const code = (option.getAttribute('data-currency') || '').toUpperCase();
                option.classList.toggle('active', code === savedCurrency);
                if (code === savedCurrency) {
                    const label = option.getAttribute('data-pref-label') || option.textContent.trim();
                    if (valueEl && label) {
                        valueEl.textContent = label;
                    }
                }
            });
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

        options.forEach((option) => {
            if (option.tagName === 'A') {
                return;
            }

            option.addEventListener('click', () => {
                if (!isCurrency) {
                    return;
                }

                const code = (option.getAttribute('data-currency') || 'TRY').toUpperCase();
                redirectCurrency(code);
            });
        });
    });

    document.addEventListener('click', () => closeAll(null));
    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape') {
            closeAll(null);
        }
    });
})();
