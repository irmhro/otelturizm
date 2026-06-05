(() => {
    const root = document.querySelector('[data-kmp-filter]');
    const grid = document.getElementById('kmp-grid');
    if (!root || !grid) return;

    const searchInput = root.querySelector('[data-kmp-search]');
    const chips = Array.from(root.querySelectorAll('[data-kmp-type]'));
    const resultEl = root.querySelector('[data-kmp-result]');
    const emptyEl = grid.querySelector('[data-kmp-empty]');
    const cards = Array.from(grid.querySelectorAll('.kmp-card[data-kmp-search]'));

    let activeType = '';

    const normalize = (value) => (value || '')
        .toLocaleLowerCase('tr-TR')
        .normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '');

    const applyFilter = () => {
        const query = normalize(searchInput?.value.trim() || '');
        let visibleCount = 0;

        cards.forEach((card) => {
            const type = card.getAttribute('data-kmp-type') || '';
            const blob = normalize(card.getAttribute('data-kmp-search') || '');
            const typeMatch = !activeType || type === activeType;
            const queryMatch = !query || blob.includes(query);
            const show = typeMatch && queryMatch;
            card.hidden = !show;
            if (show) visibleCount += 1;
        });

        if (emptyEl) {
            emptyEl.hidden = visibleCount > 0;
        }

        if (resultEl) {
            if (query || activeType) {
                resultEl.hidden = false;
                resultEl.textContent = `${visibleCount} kampanya listeleniyor`;
            } else {
                resultEl.hidden = true;
            }
        }
    };

    chips.forEach((chip) => {
        chip.addEventListener('click', () => {
            activeType = chip.getAttribute('data-kmp-type') || '';
            chips.forEach((item) => item.classList.toggle('is-active', item === chip));
            applyFilter();
        });
    });

    searchInput?.addEventListener('input', applyFilter);
})();
