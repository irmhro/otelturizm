(() => {
    window.otelturizmInitAuthAddressLookup = function (options) {
        const countrySelect = document.getElementById(options.countryId);
        const provinceSelect = document.getElementById(options.provinceId);
        const districtSelect = document.getElementById(options.districtId);
        const neighborhoodSelect = options.neighborhoodId ? document.getElementById(options.neighborhoodId) : null;
        const postalCodeInput = options.postalCodeId ? document.getElementById(options.postalCodeId) : null;
        const hiddenIds = options.hiddenIds || {};
        const hiddenCountry = hiddenIds.country ? document.getElementById(hiddenIds.country) : null;
        const hiddenProvince = hiddenIds.province ? document.getElementById(hiddenIds.province) : null;
        const hiddenDistrict = hiddenIds.district ? document.getElementById(hiddenIds.district) : null;
        const hiddenNeighborhood = hiddenIds.neighborhood ? document.getElementById(hiddenIds.neighborhood) : null;
        if (!countrySelect || !provinceSelect || !districtSelect) return Promise.resolve();

        const syncHiddenIds = function () {
            const countryOpt = countrySelect.options[countrySelect.selectedIndex];
            const provinceOpt = provinceSelect.options[provinceSelect.selectedIndex];
            const districtOpt = districtSelect.options[districtSelect.selectedIndex];
            const neighborhoodOpt = neighborhoodSelect ? neighborhoodSelect.options[neighborhoodSelect.selectedIndex] : null;
            if (hiddenCountry) hiddenCountry.value = countryOpt?.dataset.id || '';
            if (hiddenProvince) hiddenProvince.value = provinceOpt?.dataset.id || '';
            if (hiddenDistrict) hiddenDistrict.value = districtOpt?.dataset.id || '';
            if (hiddenNeighborhood) hiddenNeighborhood.value = neighborhoodOpt?.dataset.id || '';
        };

        const fillOptions = function (select, items, placeholder) {
            select.innerHTML = '';
            const placeholderOption = document.createElement('option');
            placeholderOption.value = '';
            placeholderOption.textContent = placeholder;
            select.appendChild(placeholderOption);
            (items || []).forEach(function (item) {
                const option = document.createElement('option');
                option.value = item.name;
                option.textContent = item.name;
                option.dataset.id = item.id;
                if (item.iso2) option.dataset.iso2 = item.iso2;
                if (item.postalCode) option.dataset.postalCode = item.postalCode;
                select.appendChild(option);
            });
        };

        const loadProvinces = async function () {
            const selected = countrySelect.options[countrySelect.selectedIndex];
            const countryId = selected?.dataset.id;
            if (!countryId) {
                provinceSelect.disabled = true;
                districtSelect.disabled = true;
                if (neighborhoodSelect) neighborhoodSelect.disabled = true;
                fillOptions(provinceSelect, [], 'Önce ülke seçiniz');
                fillOptions(districtSelect, [], 'Önce il seçiniz');
                if (neighborhoodSelect) fillOptions(neighborhoodSelect, [], 'Önce ilçe seçiniz');
                return;
            }
            const response = await fetch('/api/adres/iller?ulkeId=' + encodeURIComponent(countryId), { credentials: 'same-origin' });
            const items = response.ok ? await response.json() : [];
            fillOptions(provinceSelect, items, 'İl seçiniz');
            if (provinceSelect.dataset.restoreValue) {
                provinceSelect.value = provinceSelect.dataset.restoreValue;
                provinceSelect.dataset.restoreValue = '';
                await loadDistricts();
            } else {
                fillOptions(districtSelect, [], 'Önce il seçiniz');
                if (neighborhoodSelect) fillOptions(neighborhoodSelect, [], 'Önce ilçe seçiniz');
            }
            provinceSelect.disabled = false;
            districtSelect.disabled = true;
            if (neighborhoodSelect) neighborhoodSelect.disabled = true;
            syncHiddenIds();
        };

        const loadDistricts = async function () {
            const selected = provinceSelect.options[provinceSelect.selectedIndex];
            const provinceId = selected?.dataset.id;
            if (!provinceId) {
                districtSelect.disabled = true;
                if (neighborhoodSelect) neighborhoodSelect.disabled = true;
                fillOptions(districtSelect, [], 'Önce il seçiniz');
                if (neighborhoodSelect) fillOptions(neighborhoodSelect, [], 'Önce ilçe seçiniz');
                return;
            }
            const response = await fetch('/api/adres/ilceler?ilId=' + encodeURIComponent(provinceId), { credentials: 'same-origin' });
            const items = response.ok ? await response.json() : [];
            fillOptions(districtSelect, items, 'İlçe seçiniz');
            if (districtSelect.dataset.restoreValue) {
                districtSelect.value = districtSelect.dataset.restoreValue;
                districtSelect.dataset.restoreValue = '';
                await loadNeighborhoods();
            } else if (neighborhoodSelect) {
                fillOptions(neighborhoodSelect, [], 'Önce ilçe seçiniz');
            }
            districtSelect.disabled = false;
            if (neighborhoodSelect) neighborhoodSelect.disabled = !neighborhoodSelect.dataset.restoreValue;
            syncHiddenIds();
        };

        const loadNeighborhoods = async function () {
            if (!neighborhoodSelect) return;
            const selected = districtSelect.options[districtSelect.selectedIndex];
            const districtId = selected?.dataset.id;
            if (!districtId) {
                neighborhoodSelect.disabled = true;
                fillOptions(neighborhoodSelect, [], 'Önce ilçe seçiniz');
                if (postalCodeInput) postalCodeInput.value = '';
                return;
            }
            const response = await fetch('/api/adres/mahalleler?ilceId=' + encodeURIComponent(districtId), { credentials: 'same-origin' });
            const items = response.ok ? await response.json() : [];
            fillOptions(neighborhoodSelect, items, 'Mahalle seçiniz');
            if (neighborhoodSelect.dataset.restoreValue) {
                neighborhoodSelect.value = neighborhoodSelect.dataset.restoreValue;
                neighborhoodSelect.dataset.restoreValue = '';
            }
            neighborhoodSelect.disabled = false;
            syncHiddenIds();
        };

        countrySelect.addEventListener('change', function () { loadProvinces().then(syncHiddenIds); });
        provinceSelect.addEventListener('change', function () { loadDistricts().then(syncHiddenIds); });
        districtSelect.addEventListener('change', function () { loadNeighborhoods().then(syncHiddenIds); });
        if (neighborhoodSelect) {
            neighborhoodSelect.addEventListener('change', function () {
                const selected = neighborhoodSelect.options[neighborhoodSelect.selectedIndex];
                if (postalCodeInput) postalCodeInput.value = selected?.dataset.postalCode || '';
            });
        }

        return fetch('/api/adres/ulkeler', { credentials: 'same-origin' })
            .then(function (response) { return response.ok ? response.json() : []; })
            .then(function (countries) {
                fillOptions(countrySelect, countries, 'Ülke seçiniz');
                const preferredIso = (options.defaultCountryIso || 'TR').toUpperCase();
                const preferred = countries.find(function (c) { return (c.iso2 || '').toUpperCase() === preferredIso; }) || countries[0];
                if (preferred?.id) {
                    countrySelect.value = preferred.name;
                    if (!countrySelect.value) {
                        const match = Array.from(countrySelect.options).find(function (opt) { return opt.dataset.id === String(preferred.id); });
                        if (match) countrySelect.value = match.value;
                    }
                }
                return loadProvinces().then(syncHiddenIds);
            });
    };
})();
