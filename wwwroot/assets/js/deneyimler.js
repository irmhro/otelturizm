(function () {
    'use strict';

    var page = document.querySelector('[data-dxp-page]');
    if (!page) return;

    var chips = Array.from(page.querySelectorAll('.dxp-category-chip[data-dxp-category]'));
    var moodCards = Array.from(page.querySelectorAll('[data-dxp-mood]'));
    var grids = Array.from(page.querySelectorAll('[data-dxp-grid]'));
    var emptyEl = page.querySelector('[data-dxp-empty]');
    var activeCategory = 'all';

    function setActiveChip(category) {
        chips.forEach(function (chip) {
            var key = chip.getAttribute('data-dxp-category') || '';
            chip.classList.toggle('is-active', key === category);
        });
    }

    function setActiveMood(category) {
        moodCards.forEach(function (card) {
            var key = card.getAttribute('data-dxp-mood') || '';
            var active = category !== 'all' && key === category;
            card.classList.toggle('is-active', active);
            card.setAttribute('aria-pressed', active ? 'true' : 'false');
        });
    }

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

        setActiveChip(activeCategory);
        setActiveMood(activeCategory);

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
        chip.addEventListener('click', function () {
            applyFilter(chip.getAttribute('data-dxp-category') || 'all');
        });
    });

    moodCards.forEach(function (card) {
        card.addEventListener('click', function () {
            var key = card.getAttribute('data-dxp-mood') || 'all';
            if (activeCategory === key) {
                applyFilter('all');
                return;
            }
            applyFilter(key);
            var featured = page.querySelector('#dxp-one-cikan');
            if (featured && typeof featured.scrollIntoView === 'function') {
                featured.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
        });
    });

    applyFilter('all');
})();
