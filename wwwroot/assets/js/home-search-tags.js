(() => {
    const form = document.querySelector('[data-search-pro-form]');
    const tagsRoot = document.querySelector('[data-search-ai-tags]');
    if (!form || !tagsRoot) return;

    const qInput = form.querySelector('#home-q');
    const toggleBtn = tagsRoot.querySelector('[data-search-ai-toggle]');
    const panel = tagsRoot.querySelector('[data-search-ai-panel]');
    const labelEl = tagsRoot.querySelector('[data-search-ai-label]');
    const countEl = tagsRoot.querySelector('[data-search-ai-count]');
    const tagButtons = tagsRoot.querySelectorAll('[data-search-tag]');
    const selected = new Map();

    const isDesktop = () => window.matchMedia('(min-width: 901px)').matches;

    const updateCount = () => {
        if (!countEl) return;
        const n = selected.size;
        if (n > 0) {
            countEl.hidden = false;
            countEl.textContent = `${n} seçili`;
        } else {
            countEl.hidden = true;
            countEl.textContent = '';
        }
    };

    const syncPanelVisibility = (open) => {
        if (!panel) return;

        if (isDesktop()) {
            panel.hidden = false;
            if (labelEl) labelEl.hidden = !open;
            return;
        }

        panel.hidden = !open;
        if (labelEl) labelEl.hidden = false;
    };

    const setOpen = (open) => {
        tagsRoot.classList.toggle('is-collapsed', !open);
        tagsRoot.classList.toggle('is-open', open);
        if (toggleBtn) toggleBtn.setAttribute('aria-expanded', open ? 'true' : 'false');
        syncPanelVisibility(open);
    };

    if (toggleBtn && panel) {
        toggleBtn.addEventListener('click', () => {
            const isOpen = toggleBtn.getAttribute('aria-expanded') === 'true';
            setOpen(!isOpen);
        });
    }

    tagButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            const tagText = btn.getAttribute('data-search-tag') || '';
            const label = btn.textContent.trim();
            if (selected.has(label)) {
                selected.delete(label);
                btn.classList.remove('active');
                btn.setAttribute('aria-pressed', 'false');
            } else {
                selected.set(label, tagText);
                btn.classList.add('active');
                btn.setAttribute('aria-pressed', 'true');
            }
            updateCount();
        });
    });

    form.addEventListener('submit', () => {
        if (!qInput) return;
        const base = qInput.value.trim();
        const tagParts = [];
        selected.forEach(text => {
            if (text) tagParts.push(text);
        });
        const merged = [base, ...tagParts].filter(Boolean).join(', ');
        if (merged) qInput.value = merged;
    });

    window.addEventListener('resize', () => {
        const open = toggleBtn?.getAttribute('aria-expanded') === 'true';
        syncPanelVisibility(open);
    });

    setOpen(false);
})();
