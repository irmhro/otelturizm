(() => {
    document.querySelectorAll('[data-auth-tabs]').forEach(tabBar => {
        const buttons = tabBar.querySelectorAll('[data-target]');
        const scope = tabBar.closest('.auth-card') || document;
        buttons.forEach(btn => {
            btn.addEventListener('click', () => {
                const target = btn.getAttribute('data-target');
                buttons.forEach(b => b.classList.toggle('active', b === btn));
                scope.querySelectorAll('.auth-form-panel-block').forEach(panel => {
                    panel.classList.toggle('active', panel.id === target);
                });
            });
        });
    });

    document.querySelectorAll('[data-switch-tab]').forEach(btn => {
        btn.addEventListener('click', () => {
            const id = btn.getAttribute('data-switch-tab');
            document.querySelector(`[data-auth-tabs] [data-target="${id}"]`)?.click();
        });
    });

    document.querySelectorAll('[data-toggle-target]').forEach(btn => {
        btn.addEventListener('click', () => {
            const input = document.getElementById(btn.getAttribute('data-toggle-target'));
            const icon = btn.querySelector('i');
            if (!input || !icon) return;
            const show = input.type === 'password';
            input.type = show ? 'text' : 'password';
            icon.className = show ? 'far fa-eye-slash' : 'far fa-eye';
        });
    });

    const query = new URLSearchParams(window.location.search);
    if (query.get('sekme') === 'kayit' || window.location.hash === '#kayit-ol') {
        document.querySelector('[data-auth-tabs] [data-target="registerPanel"]')?.click();
    }
})();
