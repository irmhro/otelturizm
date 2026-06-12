(function () {
    'use strict';

    var configEl = document.getElementById('hotelMapConfig');
    var mapCanvas = document.getElementById('hotelMapCanvas');
    var fitBtn = document.getElementById('fitHotelMapBtn');
    var payload = document.getElementById('hotelMapPayload');
    var visibleCount = document.getElementById('hotelMapVisibleCount');
    var pinCount = document.getElementById('hotelMapPinCount');
    var summaryText = document.getElementById('hotelMapSummaryText');
    var mapHint = document.getElementById('hotelMapRadiusHint');
    var listItems = Array.from(document.querySelectorAll('[data-map-focus]'));
    var emptyState = document.getElementById('hotelMapEmptyState');
    var filterDrawer = document.getElementById('hotelMapFilterDrawer');
    var filterOverlay = document.getElementById('hotelMapFilterOverlay');
    var openFiltersBtn = document.getElementById('hotelMapOpenFilters');
    var closeFiltersBtn = document.getElementById('hotelMapCloseFilters');
    var applyFiltersBtn = document.getElementById('hotelMapApplyFilters');

    if (!mapCanvas || !window.L) return;

    var config = {};
    try {
        config = JSON.parse(configEl?.textContent || '{}');
    } catch (_) {
        config = {};
    }

    var hotelDetailPathPattern = config.hotelDetailPathPattern || '/hotel/{slug}';

    function buildHotelDetailUrl(slug) {
        return hotelDetailPathPattern.replace('{slug}', encodeURIComponent(String(slug || '').trim()));
    }

    var searchCenter = null;
    var radiusCircle = null;
    var centerMarker = null;

    function getFilterRoot() {
        var isMobile = window.matchMedia('(max-width: 900px)').matches;
        if (isMobile && filterDrawer?.classList.contains('is-open')) {
            return filterDrawer;
        }
        return document.querySelector('.hotel-map-page__sidebar') || filterDrawer || document;
    }

    function readRadiusKm(root) {
        var el = (root || getFilterRoot()).querySelector('.hotel-map-page__filter-radius');
        var value = parseInt(el?.value || '0', 10);
        return Number.isFinite(value) && value > 0 ? value : 0;
    }

    function readFilterState() {
        var root = getFilterRoot();
        var stars = Array.from(root.querySelectorAll('.hotel-map-page__star-btn.is-active'))
            .map(function (btn) { return parseInt(btn.getAttribute('data-map-star') || '0', 10); })
            .filter(function (n) { return n > 0; });
        var minEl = root.querySelector('.hotel-map-page__filter-min-price');
        var maxEl = root.querySelector('.hotel-map-page__filter-max-price');
        var minRaw = minEl?.value?.trim() || '';
        var maxRaw = maxEl?.value?.trim() || '';
        var minPrice = minRaw !== '' ? parseFloat(minRaw) || 0 : 0;
        var maxPrice = maxRaw !== '' ? parseFloat(maxRaw) || Number.MAX_SAFE_INTEGER : Number.MAX_SAFE_INTEGER;
        var priceFilterActive = minRaw !== '' || maxRaw !== '';
        return {
            query: normalizeText(root.querySelector('.hotel-map-page__filter-search')?.value || ''),
            city: normalizeText(root.querySelector('.hotel-map-page__filter-city')?.value || ''),
            district: normalizeText(root.querySelector('.hotel-map-page__filter-district')?.value || ''),
            neighborhood: normalizeText(root.querySelector('.hotel-map-page__filter-neighborhood')?.value || ''),
            stars: stars,
            minPrice: minPrice,
            maxPrice: maxPrice,
            priceFilterActive: priceFilterActive,
            radiusKm: readRadiusKm(root)
        };
    }

    function writeFilterState(state, scope) {
        var form = document.querySelector('form[data-map-filter-scope="' + scope + '"]');
        if (!form || !state) return;
        var setVal = function (selector, value) {
            var el = form.querySelector(selector);
            if (el) el.value = value || '';
        };
        setVal('.hotel-map-page__filter-search', state.queryRaw || '');
        setVal('.hotel-map-page__filter-city', state.cityRaw || '');
        setVal('.hotel-map-page__filter-district', state.districtRaw || '');
        setVal('.hotel-map-page__filter-neighborhood', state.neighborhoodRaw || '');
        if (typeof state.radiusKm === 'number') {
            setVal('.hotel-map-page__filter-radius', String(state.radiusKm || 0));
        }
        var minEl = form.querySelector('.hotel-map-page__filter-min-price');
        var maxEl = form.querySelector('.hotel-map-page__filter-max-price');
        if (minEl) minEl.value = state.priceFilterActive ? String(state.minPrice || '') : '';
        if (maxEl) maxEl.value = state.priceFilterActive && state.maxPrice !== Number.MAX_SAFE_INTEGER ? String(state.maxPrice) : '';
        form.querySelectorAll('.hotel-map-page__star-btn').forEach(function (btn) {
            var star = parseInt(btn.getAttribute('data-map-star') || '0', 10);
            btn.classList.toggle('is-active', state.stars.indexOf(star) !== -1);
        });
    }

    function syncFilterScopes(fromScope, toScope) {
        var fromForm = document.querySelector('form[data-map-filter-scope="' + fromScope + '"]');
        if (!fromForm) return;
        writeFilterState({
            queryRaw: fromForm.querySelector('.hotel-map-page__filter-search')?.value || '',
            cityRaw: fromForm.querySelector('.hotel-map-page__filter-city')?.value || '',
            districtRaw: fromForm.querySelector('.hotel-map-page__filter-district')?.value || '',
            neighborhoodRaw: fromForm.querySelector('.hotel-map-page__filter-neighborhood')?.value || '',
            radiusKm: parseInt(fromForm.querySelector('.hotel-map-page__filter-radius')?.value || '0', 10) || 0,
            minPrice: parseFloat(fromForm.querySelector('.hotel-map-page__filter-min-price')?.value || '0') || 0,
            maxPrice: parseFloat(fromForm.querySelector('.hotel-map-page__filter-max-price')?.value || '0') || Number.MAX_SAFE_INTEGER,
            priceFilterActive: !!(fromForm.querySelector('.hotel-map-page__filter-min-price')?.value || fromForm.querySelector('.hotel-map-page__filter-max-price')?.value),
            stars: Array.from(fromForm.querySelectorAll('.hotel-map-page__star-btn.is-active')).map(function (btn) {
                return parseInt(btn.getAttribute('data-map-star') || '0', 10);
            }).filter(function (n) { return n > 0; })
        }, toScope);
    }

    function openFilterDrawer() {
        syncFilterScopes('desktop', 'mobile');
        filterDrawer?.classList.add('is-open');
        filterOverlay?.classList.add('is-open');
        filterOverlay?.removeAttribute('hidden');
        filterDrawer?.setAttribute('aria-hidden', 'false');
        openFiltersBtn?.setAttribute('aria-expanded', 'true');
        document.body.style.overflow = 'hidden';
    }

    function closeFilterDrawer() {
        filterDrawer?.classList.remove('is-open');
        filterOverlay?.classList.remove('is-open');
        filterOverlay?.setAttribute('hidden', 'hidden');
        filterDrawer?.setAttribute('aria-hidden', 'true');
        openFiltersBtn?.setAttribute('aria-expanded', 'false');
        document.body.style.overflow = '';
    }

    function clearRadiusVisuals() {
        if (radiusCircle) {
            map.removeLayer(radiusCircle);
            radiusCircle = null;
        }
        if (centerMarker) {
            map.removeLayer(centerMarker);
            centerMarker = null;
        }
    }

    function setSearchCenter(lat, lon, fromGeo) {
        if (!Number.isFinite(lat) || !Number.isFinite(lon)) return;
        searchCenter = { lat: lat, lon: lon };
        clearRadiusVisuals();

        var centerIcon = window.L.divIcon({
            className: 'hotel-map-center-icon',
            html: '<span class="hotel-map-center-dot" aria-hidden="true"></span>',
            iconSize: [18, 18],
            iconAnchor: [9, 9]
        });
        centerMarker = window.L.marker([lat, lon], { icon: centerIcon, interactive: false });
        centerMarker.addTo(map);

        var state = readFilterState();
        if (state.radiusKm > 0) {
            radiusCircle = window.L.circle([lat, lon], {
                radius: state.radiusKm * 1000,
                className: 'hotel-map-radius-circle',
                color: '#E30A17',
                fillColor: '#E30A17',
                fillOpacity: 0.08,
                weight: 2
            });
            radiusCircle.addTo(map);
        }

        document.querySelectorAll('[data-map-use-geolocation]').forEach(function (btn) {
            btn.classList.toggle('is-active', !!fromGeo);
        });
        updateRadiusHint();
    }

    function clearSearchCenter() {
        searchCenter = null;
        clearRadiusVisuals();
        document.querySelectorAll('[data-map-use-geolocation]').forEach(function (btn) {
            btn.classList.remove('is-active');
        });
        updateRadiusHint();
    }

    function updateRadiusHint() {
        var state = readFilterState();
        if (!mapHint) return;
        if (state.radiusKm <= 0) {
            mapHint.hidden = true;
            mapCanvas.classList.remove('is-pick-center');
            return;
        }
        mapCanvas.classList.add('is-pick-center');
        if (!searchCenter) {
            mapHint.hidden = false;
            mapHint.textContent = state.radiusKm + ' km yarıçap — haritaya tıklayın veya Konumumu kullan';
        } else {
            mapHint.hidden = true;
        }
    }

    function resetAllFilters() {
        document.querySelectorAll('form[data-map-filter-scope]').forEach(function (form) {
            form.querySelectorAll('input[type="search"], input[type="number"]').forEach(function (el) { el.value = ''; });
            form.querySelectorAll('select').forEach(function (el) {
                if (el.classList.contains('hotel-map-page__filter-radius')) {
                    el.value = '0';
                } else {
                    el.selectedIndex = 0;
                }
            });
            form.querySelectorAll('.hotel-map-page__star-btn').forEach(function (btn) { btn.classList.remove('is-active'); });
        });
        clearSearchCenter();
        try {
            var url = new URL(window.location.href);
            url.searchParams.delete('minPrice');
            url.searchParams.delete('maxPrice');
            window.history.replaceState({}, '', url);
        } catch (_) { /* ignore */ }
        applyMapState();
    }

    var hotels = [];
    try {
        hotels = JSON.parse(payload?.textContent || '[]');
    } catch (_) {
        hotels = [];
    }

    function normalizeText(value) {
        return String(value || '')
            .toLocaleLowerCase('tr-TR')
            .replace(/\s+/g, ' ')
            .trim();
    }

    function escapeHtml(value) {
        return String(value || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function formatPrice(value) {
        var amount = Number(value || 0);
        if (!Number.isFinite(amount) || amount <= 0) return 'Fiyat yok';
        return '₺' + Math.round(amount).toLocaleString('tr-TR');
    }

    function haversineKm(lat1, lon1, lat2, lon2) {
        var R = 6371;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.sin(dLat / 2) * Math.sin(dLat / 2)
            + Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180)
            * Math.sin(dLon / 2) * Math.sin(dLon / 2);
        return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    }

    function formatDistance(km) {
        if (!Number.isFinite(km)) return '';
        if (km < 1) return Math.round(km * 1000) + ' m';
        if (km < 10) return km.toLocaleString('tr-TR', { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + ' km';
        return Math.round(km).toLocaleString('tr-TR') + ' km';
    }

    var map = window.L.map(mapCanvas, {
        zoomControl: true,
        scrollWheelZoom: true
    });

    window.L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png', {
        maxZoom: 19,
        subdomains: 'abcd',
        attribution: '&copy; OpenStreetMap katkıda bulunanlar &copy; CARTO'
    }).addTo(map);

    var clusterLayer = typeof window.L.markerClusterGroup === 'function'
        ? window.L.markerClusterGroup({
            showCoverageOnHover: false,
            zoomToBoundsOnClick: true,
            spiderfyOnMaxZoom: true,
            disableClusteringAtZoom: 15,
            maxClusterRadius: 56,
            iconCreateFunction: function (cluster) {
                var count = cluster.getChildCount();
                return window.L.divIcon({
                    className: 'hotel-map-cluster-icon',
                    html: '<span class="hotel-map-cluster" aria-label="' + count + ' otel kümesi">' + count + '</span>',
                    iconSize: [44, 44],
                    iconAnchor: [22, 22]
                });
            }
        })
        : window.L.layerGroup();
    clusterLayer.addTo(map);
    var markers = [];

    hotels.forEach(function (hotel) {
        var lat = Number(hotel.lat);
        var lon = Number(hotel.lon);
        if (!Number.isFinite(lat) || !Number.isFinite(lon)) return;

        var areaParts = [hotel.neighborhood, hotel.district, hotel.city].filter(Boolean);
        var areaText = areaParts.join(' / ');
        var priceText = formatPrice(hotel.price);
        var pillIcon = window.L.divIcon({
            className: 'hotel-map-pill-icon',
            html:
                '<button type="button" class="hotel-map-pill" aria-label="' + escapeHtml(String(hotel.name || 'Otel')) + '">' +
                    '<span>' + escapeHtml(priceText) + '</span>' +
                '</button>',
            iconSize: [74, 36],
            iconAnchor: [37, 18],
            popupAnchor: [0, -8]
        });

        var popupImage = hotel.image
            ? '<div class="hotel-map-popup__visual"><img src="' + escapeHtml(hotel.image) + '" alt="' + escapeHtml(String(hotel.name || 'Otel')) + '" loading="lazy"></div>'
            : '<div class="hotel-map-popup__visual is-placeholder"><i class="fas fa-hotel" aria-hidden="true"></i></div>';

        var popupHtml =
            '<article class="hotel-map-popup">' +
                popupImage +
                '<div class="hotel-map-popup__body">' +
                    '<strong>' + escapeHtml(String(hotel.name || 'Otel')) + '</strong>' +
                    '<span>' + escapeHtml(areaText) + '</span>' +
                    '<div class="hotel-map-popup__meta">' +
                        '<b>' + escapeHtml(priceText) + '</b>' +
                        '<em>' + escapeHtml(Number(hotel.rating || 0).toLocaleString('tr-TR', { minimumFractionDigits: 1, maximumFractionDigits: 1 })) + '</em>' +
                    '</div>' +
                    '<a class="hotel-map-popup__cta" href="' + buildHotelDetailUrl(hotel.slug || '') + '">Oteli İncele</a>' +
                '</div>' +
            '</article>';

        var marker = window.L.marker([lat, lon], { icon: pillIcon });
        marker.bindPopup(popupHtml, { closeButton: false, offset: [0, -6], maxWidth: 280, className: 'hotel-map-popup-shell' });
        markers.push({
            id: Number(hotel.id || 0),
            lat: lat,
            lon: lon,
            hotel: hotel,
            marker: marker,
            areaText: areaText,
            nameSearch: normalizeText(hotel.name),
            citySearch: normalizeText(hotel.city),
            districtSearch: normalizeText(hotel.district),
            neighborhoodSearch: normalizeText(hotel.neighborhood),
            keywordSearch: normalizeText([hotel.name, hotel.city, hotel.district, hotel.neighborhood].join(' ')),
            ratingValue: Number(hotel.rating || 0),
            starValue: Number(hotel.starCount || 0),
            priceValue: Number(hotel.price || 0)
        });
    });

    function getFilteredEntries() {
        var state = readFilterState();
        var radiusActive = state.radiusKm > 0 && searchCenter;

        var entries = markers.filter(function (entry) {
            if (state.query && entry.keywordSearch.indexOf(state.query) === -1) return false;
            if (state.city && entry.citySearch !== state.city) return false;
            if (state.district && entry.districtSearch !== state.district) return false;
            if (state.neighborhood && entry.neighborhoodSearch !== state.neighborhood) return false;
            if (state.stars.length && state.stars.indexOf(entry.starValue) === -1) return false;
            if (state.priceFilterActive && entry.priceValue > 0) {
                if (entry.priceValue < state.minPrice || entry.priceValue > state.maxPrice) return false;
            }
            if (radiusActive) {
                var dist = haversineKm(searchCenter.lat, searchCenter.lon, entry.lat, entry.lon);
                entry.distanceKm = dist;
                if (dist > state.radiusKm) return false;
            } else {
                entry.distanceKm = null;
            }
            return true;
        });

        if (radiusActive) {
            entries.sort(function (a, b) {
                return (a.distanceKm || 0) - (b.distanceKm || 0);
            });
        }

        return entries;
    }

    function fitEntries(entries, state) {
        if (state.radiusKm > 0 && searchCenter && radiusCircle) {
            map.fitBounds(radiusCircle.getBounds(), { padding: [40, 40], maxZoom: 14 });
            return;
        }

        if (!entries.length) {
            map.setView([41.015137, 28.97953], 10);
            return;
        }

        if (typeof clusterLayer.getBounds === 'function') {
            var clusterBounds = clusterLayer.getBounds();
            if (clusterBounds && clusterBounds.isValid()) {
                map.fitBounds(clusterBounds, { padding: [50, 50], maxZoom: 12 });
                return;
            }
        }

        var bounds = window.L.latLngBounds(entries.map(function (entry) {
            return [entry.lat, entry.lon];
        }));
        map.fitBounds(bounds, { padding: [50, 50], maxZoom: 12 });
    }

    function updateListItems(entries, state) {
        var radiusActive = state.radiusKm > 0 && searchCenter;
        var visibleIds = entries.map(function (e) { return e.id; });

        listItems.forEach(function (item) {
            var id = Number(item.getAttribute('data-map-id') || '0');
            var entry = entries.find(function (x) { return x.id === id; });
            var distLabel = item.querySelector('[data-distance-label]');

            if (!entry) {
                item.hidden = true;
                if (distLabel) {
                    distLabel.hidden = true;
                    distLabel.textContent = '';
                }
                return;
            }

            item.hidden = false;
            if (distLabel) {
                if (radiusActive && Number.isFinite(entry.distanceKm)) {
                    distLabel.hidden = false;
                    distLabel.textContent = formatDistance(entry.distanceKm) + ' uzaklıkta';
                } else {
                    distLabel.hidden = true;
                    distLabel.textContent = '';
                }
            }
        });

        if (radiusActive) {
            var listEl = document.getElementById('hotelMapHotelList');
            if (listEl) {
                visibleIds.forEach(function (id) {
                    var node = listEl.querySelector('[data-map-id="' + id + '"]');
                    if (node) listEl.appendChild(node);
                });
            }
        }
    }

    function applyMapState() {
        var state = readFilterState();
        updateRadiusHint();

        if (state.radiusKm > 0 && searchCenter) {
            if (radiusCircle) {
                radiusCircle.setRadius(state.radiusKm * 1000);
            } else {
                setSearchCenter(searchCenter.lat, searchCenter.lon, false);
            }
        }

        var entries = getFilteredEntries();
        clusterLayer.clearLayers();

        entries.forEach(function (entry) {
            clusterLayer.addLayer(entry.marker);
        });

        updateListItems(entries, state);

        visibleCount.textContent = String(entries.length);
        if (pinCount) {
            pinCount.textContent = String(entries.length);
        }

        var summaryParts = [entries.length + ' pin haritada'];
        if (state.radiusKm > 0) {
            if (searchCenter) {
                summaryParts.push(state.radiusKm + ' km yarıçap içinde');
            } else {
                summaryParts.push('merkez seçilmedi');
            }
        }
        summaryText.textContent = entries.length
            ? summaryParts.join(' · ') + '.'
            : 'Filtrelere uygun tesis bulunamadı. Aramayı genişletin veya filtreleri temizleyin.';

        if (emptyState) {
            var showEmpty = entries.length === 0;
            emptyState.hidden = !showEmpty;
            emptyState.classList.toggle('is-hidden', !showEmpty);
        }

        fitEntries(entries, state);
    }

    map.on('click', function (e) {
        var state = readFilterState();
        if (state.radiusKm <= 0) return;
        setSearchCenter(e.latlng.lat, e.latlng.lng, false);
        applyMapState();
    });

    fitBtn?.addEventListener('click', applyMapState);

    document.querySelectorAll('form[data-map-filter-scope]').forEach(function (form) {
        form.querySelectorAll('.hotel-map-page__filter-search, .hotel-map-page__filter-min-price, .hotel-map-page__filter-max-price').forEach(function (control) {
            control.addEventListener('input', function () {
                if (form.getAttribute('data-map-filter-scope') === 'desktop') applyMapState();
            });
        });
        form.querySelectorAll('.hotel-map-page__filter-city, .hotel-map-page__filter-district, .hotel-map-page__filter-neighborhood, .hotel-map-page__filter-radius').forEach(function (control) {
            control.addEventListener('change', function () {
                var scope = form.getAttribute('data-map-filter-scope');
                if (scope === 'desktop') {
                    applyMapState();
                }
                if (control.classList.contains('hotel-map-page__filter-radius')) {
                    var km = parseInt(control.value || '0', 10) || 0;
                    if (km <= 0) {
                        clearSearchCenter();
                    } else if (searchCenter) {
                        setSearchCenter(searchCenter.lat, searchCenter.lon, false);
                    }
                    updateRadiusHint();
                }
            });
        });
        form.querySelectorAll('.hotel-map-page__star-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                btn.classList.toggle('is-active');
                if (form.getAttribute('data-map-filter-scope') === 'desktop') applyMapState();
            });
        });
        form.querySelectorAll('[data-map-use-geolocation]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                if (!navigator.geolocation) {
                    window.alert('Tarayıcınız konum servisini desteklemiyor.');
                    return;
                }
                btn.disabled = true;
                navigator.geolocation.getCurrentPosition(function (pos) {
                    btn.disabled = false;
                    var km = readRadiusKm(form);
                    if (km <= 0) {
                        var radiusEl = form.querySelector('.hotel-map-page__filter-radius');
                        if (radiusEl) radiusEl.value = '10';
                        document.querySelectorAll('.hotel-map-page__filter-radius').forEach(function (el) {
                            el.value = '10';
                        });
                    }
                    setSearchCenter(pos.coords.latitude, pos.coords.longitude, true);
                    map.flyTo([pos.coords.latitude, pos.coords.longitude], 13, { duration: 0.5 });
                    applyMapState();
                    if (form.getAttribute('data-map-filter-scope') === 'mobile') {
                        syncFilterScopes('mobile', 'desktop');
                    }
                }, function () {
                    btn.disabled = false;
                    window.alert('Konum alınamadı. Lütfen izin verin veya haritaya tıklayarak merkez seçin.');
                }, { enableHighAccuracy: true, timeout: 12000, maximumAge: 60000 });
            });
        });
    });

    document.querySelectorAll('[data-map-filter-reset]').forEach(function (btn) {
        btn.addEventListener('click', resetAllFilters);
    });

    document.querySelector('.hotel-map-page__filter-clear')?.addEventListener('click', resetAllFilters);

    openFiltersBtn?.addEventListener('click', openFilterDrawer);
    closeFiltersBtn?.addEventListener('click', closeFilterDrawer);
    filterOverlay?.addEventListener('click', closeFilterDrawer);
    applyFiltersBtn?.addEventListener('click', function () {
        syncFilterScopes('mobile', 'desktop');
        var state = readFilterState();
        try {
            var url = new URL(window.location.href);
            if (state.priceFilterActive && state.minPrice > 0) url.searchParams.set('minPrice', String(Math.floor(state.minPrice)));
            else url.searchParams.delete('minPrice');
            if (state.priceFilterActive && state.maxPrice > 0 && state.maxPrice < Number.MAX_SAFE_INTEGER) url.searchParams.set('maxPrice', String(Math.floor(state.maxPrice)));
            else url.searchParams.delete('maxPrice');
            window.history.replaceState({}, '', url);
        } catch (_) { /* ignore */ }
        applyMapState();
        closeFilterDrawer();
    });

    listItems.forEach(function (link) {
        link.addEventListener('mouseenter', function () {
            var id = Number(link.getAttribute('data-map-id') || '0');
            var entry = markers.find(function (x) { return x.id === id; });
            if (!entry) return;
            map.flyTo([entry.lat, entry.lon], Math.max(map.getZoom(), 13), { duration: 0.45 });
            entry.marker.openPopup();
        });
    });

    applyMapState();
})();
