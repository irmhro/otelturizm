(function () {
    'use strict';

    var page = document.querySelector('[data-dxp-page]');
    if (!page) return;

    var chips = Array.from(page.querySelectorAll('[data-dxp-category]'));
    var grids = Array.from(page.querySelectorAll('[data-dxp-grid]'));
    var emptyEl = page.querySelector('[data-dxp-empty]');
    var activeCategory = 'all';

    function applyFilter(category) {
        activeCategory = category || 'all';
        var visibleCount = 0;

        grids.forEach(function (grid) {
            var cards = Array.from(grid.querySelectorAll('.dxp-card[data-dxp-category]'));
            cards.forEach(function (card) {
                var key = card.getAttribute('data-dxp-category') || '';
                var show = activeCategory === 'all' || key === activeCategory;
                card.hidden = !show;
                if (show) visibleCount++;
            });
        });

        if (emptyEl) {
            var collectionGrid = page.querySelector('#dxp-koleksiyon [data-dxp-grid]');
            if (!collectionGrid) {
                emptyEl.hidden = true;
                return;
            }
            var collectionVisible = Array.from(collectionGrid.querySelectorAll('.dxp-card[data-dxp-category]'))
                .filter(function (c) { return !c.hidden; }).length;
            emptyEl.hidden = activeCategory === 'all' || collectionVisible > 0;
        }
    }

    chips.forEach(function (chip) {
        if (!chip.matches('.dxp-category-chip')) return;
        chip.addEventListener('click', function () {
            page.querySelectorAll('.dxp-category-chip.is-active').forEach(function (el) {
                el.classList.remove('is-active');
            });
            chip.classList.add('is-active');
            applyFilter(chip.getAttribute('data-dxp-category') || 'all');
        });
    });

    applyFilter('all');
})();
