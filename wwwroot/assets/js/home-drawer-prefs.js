(() => {
    if (window.__otDrawerPrefsInit) {
        return;
    }
    window.__otDrawerPrefsInit = true;

    const accordions = document.querySelectorAll('[data-pref-accordion]');
    if (accordions.length === 0) {
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
        const valueEl = item.querySelector('[data-pref-value]');
        const options = item.querySelectorAll('.home-pref-btn');
        const isCurrency = options.length > 0 && options[0].hasAttribute('data-currency');

        if (!trigger || !panel) {
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

        options.forEach((option) => {
            if (option.tagName === 'A') {
                return;
            }

            option.addEventListener('click', () => {
                const code = (option.getAttribute('data-currency') || 'TRY').toUpperCase();
                redirectCurrency(code);
            });
        });
    });
})();
