(function () {
    'use strict';

    const page = document.getElementById('otellistePage');
    const grid = document.getElementById('otellisteHotelGrid');
    if (!page || !grid) return;

    const cards = () => Array.from(grid.querySelectorAll('.otelliste-hotel-card'));
    const countEl = document.getElementById('otellisteResultCount');
    const clientEmpty = document.getElementById('otellisteClientEmpty');
    const filterCountEl = document.getElementById('otellisteActiveFilterCount');
    const drawer = document.getElementById('otellisteFilterDrawer');
    const overlay = document.getElementById('otellisteFilterOverlay');
    const sortDesktop = document.getElementById('otellisteSortDesktop');
    const sortMobile = document.getElementById('otellisteSortMobile');

    let locationMap = { cities: [], districts: {}, neighborhoods: {} };
    try {
        const mapNode = document.getElementById('otellisteLocationMap');
        if (mapNode?.textContent) locationMap = JSON.parse(mapNode.textContent);
    } catch (_) { /* ignore */ }

    function normalize(value) {
        return (value || '')
            .toString()
            .replace(/ı/g, 'i')
            .replace(/İ/g, 'i')
            .replace(/ö/g, 'o')
            .replace(/Ö/g, 'o')
            .replace(/ü/g, 'u')
            .replace(/Ü/g, 'u')
            .replace(/ş/g, 's')
            .replace(/Ş/g, 's')
            .replace(/ç/g, 'c')
            .replace(/Ç/g, 'c')
            .replace(/ğ/g, 'g')
            .replace(/Ğ/g, 'g')
            .toLowerCase()
            .trim();
    }

    function getScopeRoot(scope) {
        return page.querySelector('.otelliste-filters[data-filter-scope="' + scope + '"]');
    }

    function readScope(scope) {
        const root = getScopeRoot(scope);
        if (!root) return null;
        const stars = Array.from(root.querySelectorAll('.otelliste-star-btn.is-active'))
            .map(btn => parseInt(btn.getAttribute('data-star') || '0', 10))
            .filter(n => n > 0);
        const amenities = Array.from(root.querySelectorAll('.otelliste-filter-amenity:checked'))
            .map(el => normalize(el.value));
        const campaigns = Array.from(root.querySelectorAll('.otelliste-filter-campaign:checked'))
            .map(el => normalize(el.value));
        const minPriceEl = root.querySelector('.otelliste-filter-min-price');
        const maxPriceEl = root.querySelector('.otelliste-filter-max-price');
        return {
            keyword: normalize(root.querySelector('.otelliste-filter-keyword')?.value || ''),
            city: normalize(root.querySelector('.otelliste-filter-city')?.value || ''),
            district: normalize(root.querySelector('.otelliste-filter-district')?.value || ''),
            neighborhood: normalize(root.querySelector('.otelliste-filter-neighborhood')?.value || ''),
            minPrice: minPriceEl ? parseFloat(minPriceEl.value) || 0 : 0,
            maxPrice: maxPriceEl ? parseFloat(maxPriceEl.value) || Number.MAX_SAFE_INTEGER : Number.MAX_SAFE_INTEGER,
            stars,
            amenities,
            campaigns
        };
    }

    function writeScope(scope, state) {
        const root = getScopeRoot(scope);
        if (!root || !state) return;
        const setVal = (sel, val) => {
            const el = root.querySelector(sel);
            if (el) el.value = val || '';
        };
        setVal('.otelliste-filter-keyword', state.keyword);
        setVal('.otelliste-filter-city', state.cityRaw || '');
        setVal('.otelliste-filter-district', state.districtRaw || '');
        setVal('.otelliste-filter-neighborhood', state.neighborhoodRaw || '');
        const minPriceEl = root.querySelector('.otelliste-filter-min-price');
        const maxPriceEl = root.querySelector('.otelliste-filter-max-price');
        if (minPriceEl && state.minPrice != null) minPriceEl.value = state.minPrice;
        if (maxPriceEl && state.maxPrice != null) maxPriceEl.value = state.maxPrice;
        root.querySelectorAll('.otelliste-star-btn').forEach(btn => {
            const star = parseInt(btn.getAttribute('data-star') || '0', 10);
            btn.classList.toggle('is-active', state.stars.includes(star));
        });
        root.querySelectorAll('.otelliste-filter-amenity').forEach(ch => {
            ch.checked = state.amenities.includes(normalize(ch.value));
        });
        root.querySelectorAll('.otelliste-filter-campaign').forEach(ch => {
            ch.checked = state.campaigns.includes(normalize(ch.value));
        });
    }

    function readFilters() {
        const desktop = readScope('desktop');
        const mobile = readScope('mobile');
        const active = window.matchMedia('(max-width: 900px)').matches && drawer?.classList.contains('is-open')
            ? mobile
            : desktop;
        return active || desktop || mobile || {
            keyword: '', city: '', district: '', neighborhood: '',
            minPrice: 0, maxPrice: Number.MAX_SAFE_INTEGER,
            stars: [], amenities: [], campaigns: []
        };
    }

    function syncSelectOptions(scope) {
        const root = getScopeRoot(scope);
        if (!root) return;
        const citySel = root.querySelector('.otelliste-filter-city');
        const districtSel = root.querySelector('.otelliste-filter-district');
        const neighborhoodSel = root.querySelector('.otelliste-filter-neighborhood');
        if (!citySel || !districtSel || !neighborhoodSel) return;

        const selectedCity = citySel.value;
        const selectedDistrict = districtSel.value;
        const districtsForCity = selectedCity && locationMap.districts?.[selectedCity]
            ? locationMap.districts[selectedCity]
            : Object.values(locationMap.districts || {}).flat().filter((v, i, a) => a.indexOf(v) === i).sort();

        districtSel.innerHTML = '<option value="">Tüm ilçeler</option>';
        districtsForCity.forEach(d => {
            const opt = document.createElement('option');
            opt.value = d;
            opt.textContent = d;
            if (d === selectedDistrict) opt.selected = true;
            districtSel.appendChild(opt);
        });

        const nbKey = selectedCity && selectedDistrict ? selectedCity + '|' + selectedDistrict : '';
        const neighborhoods = nbKey && locationMap.neighborhoods?.[nbKey]
            ? locationMap.neighborhoods[nbKey]
            : Object.values(locationMap.neighborhoods || {}).flat().filter((v, i, a) => a.indexOf(v) === i).sort();
        const selectedNb = neighborhoodSel.value;
        neighborhoodSel.innerHTML = '<option value="">Tüm mahalleler</option>';
        neighborhoods.forEach(n => {
            const opt = document.createElement('option');
            opt.value = n;
            opt.textContent = n;
            if (n === selectedNb) opt.selected = true;
            neighborhoodSel.appendChild(opt);
        });
    }

    function countActiveFilters(state) {
        let n = 0;
        if (state.keyword) n++;
        if (state.city) n++;
        if (state.district) n++;
        if (state.neighborhood) n++;
        if (state.stars.length) n++;
        if (state.amenities.length) n++;
        if (state.campaigns.length) n++;
        return n;
    }

    function cardMatches(card, state) {
        if (state.keyword) {
            const keywords = card.getAttribute('data-keywords') || '';
            if (!keywords.includes(state.keyword)) return false;
        }
        if (state.city && normalize(card.getAttribute('data-city')) !== state.city) return false;
        if (state.district && normalize(card.getAttribute('data-district')) !== state.district) return false;
        if (state.neighborhood && normalize(card.getAttribute('data-neighborhood')) !== state.neighborhood) return false;

        const price = parseFloat(card.getAttribute('data-price') || '0');
        if (price > 0 && (price < state.minPrice || price > state.maxPrice)) return false;

        const stars = parseInt(card.getAttribute('data-stars') || '0', 10);
        if (state.stars.length && !state.stars.includes(stars)) return false;

        if (state.amenities.length) {
            const cardAmenities = (card.getAttribute('data-amenities') || '').split(/\s+/).filter(Boolean);
            if (!state.amenities.every(a => cardAmenities.includes(a))) return false;
        }

        if (state.campaigns.length) {
            const cardCampaigns = (card.getAttribute('data-campaign-slugs') || '').split(/\s+/).filter(Boolean);
            if (!state.campaigns.some(c => cardCampaigns.includes(c))) return false;
        }

        return true;
    }

    function getSortValue() {
        return sortDesktop?.value || sortMobile?.value || 'recommended';
    }

    function sortVisible(list) {
        const mode = getSortValue();
        return list.sort((a, b) => {
            const pa = parseFloat(a.getAttribute('data-price') || '0');
            const pb = parseFloat(b.getAttribute('data-price') || '0');
            const ra = parseFloat(a.getAttribute('data-rating') || '0');
            const rb = parseFloat(b.getAttribute('data-rating') || '0');
            const fa = a.getAttribute('data-featured') === '1';
            const fb = b.getAttribute('data-featured') === '1';
            if (mode === 'price-asc') return (pa || Infinity) - (pb || Infinity);
            if (mode === 'price-desc') return (pb || 0) - (pa || 0);
            if (mode === 'rating-desc') return rb - ra;
            if (fa !== fb) return fa ? -1 : 1;
            return rb - ra;
        });
    }

    function applyFilters() {
        const state = readFilters();
        const allCards = cards();
        let visible = 0;
        allCards.forEach(card => {
            const show = cardMatches(card, state);
            card.hidden = !show;
            if (show) visible++;
        });

        sortVisible(allCards.filter(c => !c.hidden)).forEach(card => grid.appendChild(card));

        if (countEl) countEl.textContent = visible + ' otel';
        if (clientEmpty) clientEmpty.hidden = visible > 0 || allCards.length === 0;

        const activeCount = countActiveFilters(state);
        if (filterCountEl) {
            filterCountEl.textContent = activeCount > 0 ? String(activeCount) : '';
            filterCountEl.setAttribute('data-count', String(activeCount));
        }
    }

    function syncSort(from, to) {
        if (from && to && from.value !== to.value) to.value = from.value;
    }

    function resetFilters() {
        ['desktop', 'mobile'].forEach(scope => {
            const root = getScopeRoot(scope);
            if (!root) return;
            root.querySelectorAll('input[type="search"], input[type="text"]').forEach(el => { el.value = ''; });
            root.querySelectorAll('select').forEach(el => { el.selectedIndex = 0; });
            root.querySelectorAll('.otelliste-star-btn').forEach(btn => btn.classList.remove('is-active'));
            root.querySelectorAll('input[type="checkbox"]').forEach(ch => { ch.checked = false; });
        });
        applyFilters();
    }

    function openDrawer() {
        const desktop = readScope('desktop');
        if (desktop) {
            writeScope('mobile', {
                keyword: desktop.keyword,
                cityRaw: getScopeRoot('desktop')?.querySelector('.otelliste-filter-city')?.value,
                districtRaw: getScopeRoot('desktop')?.querySelector('.otelliste-filter-district')?.value,
                neighborhoodRaw: getScopeRoot('desktop')?.querySelector('.otelliste-filter-neighborhood')?.value,
                minPrice: desktop.minPrice,
                maxPrice: desktop.maxPrice === Number.MAX_SAFE_INTEGER ? '' : desktop.maxPrice,
                stars: desktop.stars,
                amenities: desktop.amenities,
                campaigns: desktop.campaigns
            });
        }
        syncSelectOptions('mobile');
        drawer?.classList.add('is-open');
        overlay?.classList.add('is-open');
        overlay?.removeAttribute('hidden');
        drawer?.setAttribute('aria-hidden', 'false');
        document.body.style.overflow = 'hidden';
    }

    function closeDrawer() {
        drawer?.classList.remove('is-open');
        overlay?.classList.remove('is-open');
        overlay?.setAttribute('hidden', 'hidden');
        drawer?.setAttribute('aria-hidden', 'true');
        document.body.style.overflow = '';
    }

    function applyMobileToDesktop() {
        const mobile = readScope('mobile');
        if (!mobile) return;
        writeScope('desktop', {
            keyword: mobile.keyword,
            cityRaw: getScopeRoot('mobile')?.querySelector('.otelliste-filter-city')?.value,
            districtRaw: getScopeRoot('mobile')?.querySelector('.otelliste-filter-district')?.value,
            neighborhoodRaw: getScopeRoot('mobile')?.querySelector('.otelliste-filter-neighborhood')?.value,
            minPrice: mobile.minPrice,
            maxPrice: mobile.maxPrice === Number.MAX_SAFE_INTEGER ? '' : mobile.maxPrice,
            stars: mobile.stars,
            amenities: mobile.amenities,
            campaigns: mobile.campaigns
        });
        syncSelectOptions('desktop');
        applyFilters();
        closeDrawer();
    }

    function bindScope(scope) {
        const root = getScopeRoot(scope);
        if (!root) return;
        root.querySelectorAll('.otelliste-filter-keyword, .otelliste-filter-min-price, .otelliste-filter-max-price').forEach(el => {
            el.addEventListener('input', () => {
                if (scope === 'desktop') applyFilters();
            });
        });
        root.querySelectorAll('.otelliste-filter-city, .otelliste-filter-district, .otelliste-filter-neighborhood').forEach(el => {
            el.addEventListener('change', () => {
                syncSelectOptions(scope);
                if (scope === 'desktop') applyFilters();
            });
        });
        root.querySelectorAll('.otelliste-filter-amenity, .otelliste-filter-campaign').forEach(el => {
            el.addEventListener('change', () => {
                if (scope === 'desktop') applyFilters();
            });
        });
        root.querySelectorAll('.otelliste-star-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                btn.classList.toggle('is-active');
                if (scope === 'desktop') applyFilters();
            });
        });
        root.querySelectorAll('.otelliste-filter-reset').forEach(btn => {
            btn.addEventListener('click', resetFilters);
        });
        root.querySelectorAll('.otelliste-filter-apply').forEach(btn => {
            btn.addEventListener('click', applyMobileToDesktop);
        });
    }

    document.getElementById('otellisteOpenDrawer')?.addEventListener('click', openDrawer);
    document.getElementById('otellisteCloseDrawer')?.addEventListener('click', closeDrawer);
    document.getElementById('otellisteApplyDrawer')?.addEventListener('click', applyMobileToDesktop);
    overlay?.addEventListener('click', closeDrawer);

    sortDesktop?.addEventListener('change', () => {
        syncSort(sortDesktop, sortMobile);
        applyFilters();
    });
    sortMobile?.addEventListener('change', () => {
        syncSort(sortMobile, sortDesktop);
        applyFilters();
    });

    page.querySelectorAll('.otelliste-filter-reset').forEach(btn => btn.addEventListener('click', resetFilters));

    bindScope('desktop');
    bindScope('mobile');
    syncSelectOptions('desktop');
    syncSelectOptions('mobile');
    applyFilters();
})();
