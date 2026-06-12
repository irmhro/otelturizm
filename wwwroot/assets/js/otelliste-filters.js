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
    let travelPrefs = null;
    let travelPrefActive = false;
    let travelPrefRelaxed = false;
    try {
        const mapNode = document.getElementById('otellisteLocationMap');
        if (mapNode?.textContent) locationMap = JSON.parse(mapNode.textContent);
    } catch (_) { /* ignore */ }
    try {
        const travelNode = document.getElementById('otellisteTravelPreferences');
        if (travelNode?.textContent) travelPrefs = JSON.parse(travelNode.textContent);
    } catch (_) { /* ignore */ }

    const TRAVEL_PREF_TOKEN_MAP = {
        room: {
            'standart oda': ['standart', 'standard', 'ekonomi', 'classic'],
            'deluxe oda': ['deluxe', 'superior', 'premium'],
            'suit oda': ['suit', 'suite', 'jakuzi'],
            'aile odasi': ['aile', 'family', 'triple', 'connected'],
            'sessiz kat': ['sessiz', 'quiet', 'sakin'],
            'sigara icilmeyen oda': ['sigara', 'smoke', 'sigara icilmez', 'non smoking']
        },
        bed: {
            'tek buyuk yatak': ['king', 'queen', 'double', 'tek', 'cift kisilik'],
            'iki ayri yatak': ['twin', 'iki', 'ayri', 'single'],
            'cift kisilik yatak': ['double', 'cift', 'queen'],
            'aile yatagi': ['aile', 'family', 'triple'],
            'ek yatak uygun olsun': ['ek yatak', 'extra', 'sofa']
        },
        purpose: {
            'is': ['is', 'business', 'kongre', 'toplanti', 'merkez', 'sehir'],
            'tatil': ['tatil', 'holiday', 'havuz', 'plaj', 'spa', 'dinlen'],
            'aile ziyareti': ['aile', 'family'],
            'saglik': ['spa', 'wellness', 'termal', 'saglik'],
            'etkinlik': ['etkinlik', 'kongre', 'dugun', 'event']
        },
        lang: {
            'turkce': ['turkce', 'turkish'],
            'ingilizce': ['english', 'ingilizce'],
            'almanca': ['german', 'almanca', 'deutsch'],
            'fransizca': ['french', 'fransizca'],
            'arapca': ['arabic', 'arapca'],
            'rusca': ['russian', 'rusca']
        }
    };

    function prefIsActive(value) {
        const v = normalize(value);
        return !!v && v !== 'fark etmez' && v !== 'karisik';
    }

    function parseTravelPurposes(value) {
        const raw = (value || '').trim();
        if (!raw || normalize(raw) === 'karisik') return [];
        return raw.split('|').map(part => part.trim()).filter(Boolean);
    }

    function prefSearchTokens(category, value) {
        const key = normalize(value);
        if (!key) return [];
        const mapped = TRAVEL_PREF_TOKEN_MAP[category]?.[key];
        if (mapped?.length) return mapped.map(normalize);
        return key.split(/\s+/).filter(Boolean);
    }

    function cardTravelHaystack(card) {
        return normalize(
            (card.getAttribute('data-travel-match') || '') + ' ' +
            (card.getAttribute('data-keywords') || '') + ' ' +
            (card.getAttribute('data-amenities') || '')
        );
    }

    function cardMatchesTravelPreference(card, relaxed) {
        if (!travelPrefActive || !travelPrefs) return true;

        const haystack = cardTravelHaystack(card);
        const checks = [];

        if (prefIsActive(travelPrefs.roomPreference)) {
            checks.push(prefSearchTokens('room', travelPrefs.roomPreference));
        }
        if (prefIsActive(travelPrefs.bedPreference)) {
            checks.push(prefSearchTokens('bed', travelPrefs.bedPreference));
        }
        const travelPurposes = parseTravelPurposes(travelPrefs.travelPurpose);
        if (travelPurposes.length) {
            const purposeTokens = travelPurposes.flatMap(purpose => prefSearchTokens('purpose', purpose));
            if (purposeTokens.length) checks.push(purposeTokens);
        }
        if (prefIsActive(travelPrefs.spokenLanguages)) {
            checks.push(prefSearchTokens('lang', travelPrefs.spokenLanguages));
        }
        if (travelPrefs.specialRequests) {
            const words = normalize(travelPrefs.specialRequests).split(/\s+/).filter(w => w.length > 2);
            if (words.length) checks.push(words);
        }

        if (!checks.length) return true;

        const matchGroup = (tokens) => tokens.some(token => haystack.includes(token));
        if (relaxed) {
            return checks.some(matchGroup);
        }
        return checks.every(matchGroup);
    }

    function setTravelPrefButtons(active) {
        page.querySelectorAll('[data-travel-pref-apply]').forEach(btn => {
            btn.classList.toggle('is-active', active);
            btn.setAttribute('aria-pressed', active ? 'true' : 'false');
        });
    }

    function showTravelPrefToast(message) {
        page.querySelectorAll('.otelliste-travel-pref-toast').forEach(node => {
            node.textContent = message || '';
            node.classList.toggle('is-visible', !!message);
        });
    }

    function toggleTravelPrefSearch() {
        if (!travelPrefs?.hasActionablePreferences) return;
        travelPrefActive = !travelPrefActive;
        travelPrefRelaxed = false;
        setTravelPrefButtons(travelPrefActive);
        if (!travelPrefActive) {
            showTravelPrefToast('');
            applyFilters();
            return;
        }

        applyFilters();
        let visible = cards().filter(card => !card.hidden).length;
        if (visible === 0) {
            travelPrefRelaxed = true;
            applyFilters();
            visible = cards().filter(card => !card.hidden).length;
            showTravelPrefToast(
                visible > 0
                    ? 'Tam eşleşme bulunamadı; yakın sonuçlar gösteriliyor.'
                    : 'Seyahat tercihlerinize uygun otel bulunamadı.'
            );
            return;
        }

        showTravelPrefToast('Seyahat tercihlerinize uygun oteller listeleniyor.');
    }

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
        const smartRoutes = Array.from(root.querySelectorAll('.otelliste-filter-smart-route-input:checked'))
            .map(el => normalize(el.value));
        const minPriceEl = root.querySelector('.otelliste-filter-min-price');
        const maxPriceEl = root.querySelector('.otelliste-filter-max-price');
        const minRaw = minPriceEl?.value?.trim() ?? '';
        const maxRaw = maxPriceEl?.value?.trim() ?? '';
        const defaultMin = minPriceEl ? parseFloat(minPriceEl.getAttribute('data-default-min') || '0') || 0 : 0;
        const defaultMax = maxPriceEl ? parseFloat(maxPriceEl.getAttribute('data-default-max') || '0') || Number.MAX_SAFE_INTEGER : Number.MAX_SAFE_INTEGER;
        const minPrice = minRaw !== '' ? parseFloat(minRaw) || 0 : 0;
        const maxPrice = maxRaw !== '' ? parseFloat(maxRaw) || Number.MAX_SAFE_INTEGER : Number.MAX_SAFE_INTEGER;
        const priceFilterActive = minRaw !== '' || maxRaw !== '';
        return {
            keyword: normalize(root.querySelector('.otelliste-filter-keyword')?.value || ''),
            city: normalize(root.querySelector('.otelliste-filter-city')?.value || ''),
            district: normalize(root.querySelector('.otelliste-filter-district')?.value || ''),
            neighborhood: normalize(root.querySelector('.otelliste-filter-neighborhood')?.value || ''),
            minPrice,
            maxPrice,
            defaultMin,
            defaultMax,
            priceFilterActive,
            stars,
            smartRoutes
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
        if (minPriceEl) {
            minPriceEl.value = state.priceFilterActive ? String(state.minPrice ?? '') : '';
        }
        if (maxPriceEl) {
            maxPriceEl.value = state.priceFilterActive && state.maxPrice !== Number.MAX_SAFE_INTEGER
                ? String(state.maxPrice)
                : '';
        }
        root.querySelectorAll('.otelliste-star-btn').forEach(btn => {
            const star = parseInt(btn.getAttribute('data-star') || '0', 10);
            btn.classList.toggle('is-active', state.stars.includes(star));
        });
        root.querySelectorAll('.otelliste-filter-smart-route-input').forEach(ch => {
            ch.checked = state.smartRoutes.includes(normalize(ch.value));
            ch.closest('.otelliste-filter-smart-route')?.classList.toggle('active', ch.checked);
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
            minPrice: 0, maxPrice: Number.MAX_SAFE_INTEGER, priceFilterActive: false,
            stars: [], smartRoutes: []
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
        if (state.smartRoutes.length) n++;
        if (state.priceFilterActive) n++;
        if (travelPrefActive) n++;
        return n;
    }

    function cardMatches(card, state) {
        if (!cardMatchesTravelPreference(card, travelPrefRelaxed)) return false;
        if (state.keyword) {
            const keywords = card.getAttribute('data-keywords') || '';
            if (!keywords.includes(state.keyword)) return false;
        }
        if (state.city && normalize(card.getAttribute('data-city')) !== state.city) return false;
        if (state.district && normalize(card.getAttribute('data-district')) !== state.district) return false;
        if (state.neighborhood && normalize(card.getAttribute('data-neighborhood')) !== state.neighborhood) return false;

        const price = parseFloat(card.getAttribute('data-price') || '0');
        if (state.priceFilterActive && price > 0 && (price < state.minPrice || price > state.maxPrice)) return false;

        const stars = parseInt(card.getAttribute('data-stars') || '0', 10);
        if (state.stars.length && !state.stars.includes(stars)) return false;

        if (state.smartRoutes.length) {
            const cardRoutes = (card.getAttribute('data-smart-routes') || '').split(/\s+/).filter(Boolean);
            if (!state.smartRoutes.some(r => cardRoutes.includes(r))) return false;
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
            filterCountEl.setAttribute('aria-label', activeCount > 0 ? activeCount + ' aktif filtre' : 'Aktif filtre yok');
        }
    }

    function syncSort(from, to) {
        if (from && to && from.value !== to.value) to.value = from.value;
        persistSort(getSortValue());
    }

    function persistSort(mode) {
        try {
            const url = new URL(window.location.href);
            if (mode && mode !== 'recommended') url.searchParams.set('sort', mode);
            else url.searchParams.delete('sort');
            window.history.replaceState({}, '', url);
        } catch (_) { /* ignore */ }
    }

    function hydrateSortFromQuery() {
        const mode = new URLSearchParams(window.location.search).get('sort');
        if (!mode) return;
        const allowed = ['recommended', 'price-asc', 'price-desc', 'rating-desc'];
        if (!allowed.includes(mode)) return;
        if (sortDesktop) sortDesktop.value = mode;
        if (sortMobile) sortMobile.value = mode;
    }

    function resetFilters() {
        travelPrefActive = false;
        travelPrefRelaxed = false;
        setTravelPrefButtons(false);
        showTravelPrefToast('');
        ['desktop', 'mobile'].forEach(scope => {
            const root = getScopeRoot(scope);
            if (!root) return;
            root.querySelectorAll('input[type="search"], input[type="text"], input[type="number"]').forEach(el => { el.value = ''; });
            root.querySelectorAll('select').forEach(el => { el.selectedIndex = 0; });
            root.querySelectorAll('.otelliste-star-btn').forEach(btn => btn.classList.remove('is-active'));
            root.querySelectorAll('input[type="checkbox"]').forEach(ch => { ch.checked = false; });
        });
        try {
            const url = new URL(window.location.href);
            url.searchParams.delete('minPrice');
            url.searchParams.delete('maxPrice');
            url.searchParams.delete('page');
            window.history.replaceState({}, '', url);
        } catch (_) { /* ignore */ }
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
                maxPrice: desktop.maxPrice,
                priceFilterActive: desktop.priceFilterActive,
                stars: desktop.stars,
                smartRoutes: desktop.smartRoutes
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

    function persistPriceQuery(state) {
        try {
            const url = new URL(window.location.href);
            if (state.priceFilterActive && state.minPrice > 0) {
                url.searchParams.set('minPrice', String(Math.floor(state.minPrice)));
            } else {
                url.searchParams.delete('minPrice');
            }
            if (state.priceFilterActive && state.maxPrice > 0 && state.maxPrice < Number.MAX_SAFE_INTEGER) {
                url.searchParams.set('maxPrice', String(Math.floor(state.maxPrice)));
            } else {
                url.searchParams.delete('maxPrice');
            }
            url.searchParams.delete('page');
            window.history.replaceState({}, '', url);
        } catch (_) { /* ignore */ }
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
            maxPrice: mobile.maxPrice,
            priceFilterActive: mobile.priceFilterActive,
            stars: mobile.stars,
            smartRoutes: mobile.smartRoutes
        });
        syncSelectOptions('desktop');
        persistPriceQuery(mobile);
        if (mobile.priceFilterActive) {
            window.location.reload();
            return;
        }
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
        root.querySelectorAll('.otelliste-filter-smart-route-input').forEach(el => {
            el.addEventListener('change', () => {
                el.closest('.otelliste-filter-smart-route')?.classList.toggle('active', el.checked);
                if (scope === 'desktop') applyFilters();
            });
        });
        root.querySelectorAll('.otelliste-star-btn').forEach(btn => {
            btn.setAttribute('tabindex', '0');
            btn.setAttribute('role', 'button');
            const toggle = () => {
                btn.classList.toggle('is-active');
                if (scope === 'desktop') applyFilters();
            };
            btn.addEventListener('click', toggle);
            btn.addEventListener('keydown', (e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    toggle();
                }
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

    (function initDrawerSwipeDismiss() {
        if (!drawer || !window.matchMedia('(max-width: 900px)').matches) {
            return;
        }

        let startY = 0;
        let tracking = false;

        drawer.addEventListener('touchstart', (event) => {
            if (!drawer.classList.contains('is-open') || !event.touches || event.touches.length !== 1) {
                return;
            }
            const body = drawer.querySelector('.otelliste-filter-drawer-body');
            if (body && body.scrollTop > 8) {
                return;
            }
            startY = event.touches[0].clientY;
            tracking = true;
        }, { passive: true });

        drawer.addEventListener('touchmove', (event) => {
            if (!tracking || !event.touches || event.touches.length !== 1) {
                return;
            }
            const deltaY = event.touches[0].clientY - startY;
            if (deltaY > 72) {
                tracking = false;
                closeDrawer();
            }
        }, { passive: true });

        drawer.addEventListener('touchend', () => {
            tracking = false;
        }, { passive: true });
    })();

    sortDesktop?.addEventListener('change', () => {
        syncSort(sortDesktop, sortMobile);
        applyFilters();
    });
    sortMobile?.addEventListener('change', () => {
        syncSort(sortMobile, sortDesktop);
        applyFilters();
    });

    page.querySelectorAll('[data-travel-pref-apply]').forEach(btn => {
        btn.addEventListener('click', toggleTravelPrefSearch);
    });

    page.querySelectorAll('.otelliste-filter-reset').forEach(btn => btn.addEventListener('click', resetFilters));

    bindScope('desktop');
    bindScope('mobile');
    syncSelectOptions('desktop');
    syncSelectOptions('mobile');

    (function hydrateFromQuery() {
        const params = new URLSearchParams(window.location.search);
        const q = params.get('q') || params.get('city') || '';
        const minPrice = params.get('minPrice') || '';
        const maxPrice = params.get('maxPrice') || '';
        ['desktop', 'mobile'].forEach(scope => {
            const root = getScopeRoot(scope);
            if (!root) return;
            const keyword = root.querySelector('.otelliste-filter-keyword');
            if (keyword && q && !keyword.value) keyword.value = q;
            const minEl = root.querySelector('.otelliste-filter-min-price');
            const maxEl = root.querySelector('.otelliste-filter-max-price');
            if (minEl && minPrice && !minEl.value) minEl.value = minPrice;
            if (maxEl && maxPrice && !maxEl.value) maxEl.value = maxPrice;
        });
    })();

    hydrateSortFromQuery();

    (function initCardGalleryHover() {
        if (!window.matchMedia('(hover: hover) and (min-width: 901px)').matches) return;
        grid.querySelectorAll('.otelliste-hotel-card').forEach(card => {
            const raw = card.getAttribute('data-gallery');
            if (!raw) return;
            let images;
            try { images = JSON.parse(raw); } catch (_) { return; }
            if (!Array.isArray(images) || images.length < 2) return;
            const img = card.querySelector('.otelliste-card-media img');
            if (!img) return;
            const original = img.getAttribute('src') || img.src;
            let idx = 1;
            let timer;
            card.addEventListener('mouseenter', () => {
                idx = 1;
                timer = window.setInterval(() => {
                    img.classList.add('is-gallery-fading');
                    window.setTimeout(() => {
                        img.src = images[idx % images.length];
                        img.classList.remove('is-gallery-fading');
                        idx += 1;
                    }, 180);
                }, 5000);
            });
            card.addEventListener('mouseleave', () => {
                if (timer) window.clearInterval(timer);
                img.src = original;
            });
        });
    })();

    document.querySelectorAll('[data-amenity-more]').forEach((btn) => {
        btn.addEventListener('click', (event) => {
            event.preventDefault();
            event.stopPropagation();
            const wrap = btn.closest('.otelliste-card-amenities');
            if (wrap) {
                wrap.setAttribute('data-amenity-expanded', 'true');
            }
        });
    });

    applyFilters();
})();
