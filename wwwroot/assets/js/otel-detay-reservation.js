(function () {
    'use strict';

    const form = document.getElementById('bookingForm');
    if (!(form instanceof HTMLFormElement)) return;

    const roomItemsRoot = document.getElementById('roomItems');
    const roomItemTemplate = document.getElementById('roomItemTemplate');
    const roomsJsonInput = document.getElementById('roomsJsonInput');
    const roomTypeIdInput = document.getElementById('roomTypeIdInput');
    const checkInInput = document.getElementById('checkInDateInput');
    const checkOutInput = document.getElementById('checkOutDateInput');
    const roomCountInput = document.getElementById('roomCountInput');
    const adultCountInput = document.getElementById('adultCountInput');
    const paymentMethodSelect = document.getElementById('paymentMethodSelect');
    const bookingSidebar = document.getElementById('bookingSidebar');
    const bookingBackdrop = document.getElementById('bookingModalBackdrop');
    const submitButton = form.querySelector('[data-reservation-create-button]');
    const addRoomButton = document.getElementById('addRoomBtn');
    const confirmModal = document.getElementById('reservationConfirmModal');
    const confirmSummary = document.getElementById('reservationConfirmSummary');
    const confirmButton = document.getElementById('reservationConfirmSubmitBtn');
    const infoModal = document.getElementById('detailInfoModal');
    const infoModalText = document.getElementById('detailInfoModalText');
    const infoModalActions = document.getElementById('detailInfoModalActions');
    const infoModalTitle = document.getElementById('detailInfoModalTitle');
    const actionBackdrop = document.getElementById('detailActionBackdrop');
    let pendingSubmit = false;

    function clampRoomCount(value) {
        const parsed = parseInt(value || '1', 10) || 1;
        return Math.min(20, Math.max(1, parsed));
    }

    function roomNodes() {
        return roomItemsRoot ? Array.from(roomItemsRoot.querySelectorAll('[data-room-item]')) : [];
    }

    function readRoom(item) {
        const select = item?.querySelector('[data-field="roomType"]');
        const checkIn = item?.querySelector('[data-field="checkIn"]');
        const checkOut = item?.querySelector('[data-field="checkOut"]');
        const roomCount = item?.querySelector('[data-field="roomCount"]');
        const option = select?.selectedOptions?.[0];
        return {
            roomTypeId: parseInt(select?.value || '0', 10) || 0,
            roomName: option?.dataset.roomName || option?.textContent?.trim() || 'Seçilen oda',
            checkIn: checkIn?.value || '',
            checkOut: checkOut?.value || '',
            roomCount: clampRoomCount(roomCount?.value)
        };
    }

    function normalizeDates(item) {
        const checkIn = item?.querySelector('[data-field="checkIn"]');
        const checkOut = item?.querySelector('[data-field="checkOut"]');
        if (!(checkIn instanceof HTMLInputElement) || !(checkOut instanceof HTMLInputElement)) return;

        const today = new Date();
        today.setHours(0, 0, 0, 0);
        const todayText = today.toISOString().slice(0, 10);
        if (!checkIn.value || checkIn.value < todayText) checkIn.value = todayText;

        const inDate = new Date(checkIn.value + 'T00:00:00');
        if (Number.isNaN(inDate.getTime())) return;
        const outDate = new Date(inDate);
        outDate.setDate(outDate.getDate() + 1);
        const minOut = outDate.toISOString().slice(0, 10);
        checkOut.min = minOut;
        if (!checkOut.value || checkOut.value <= checkIn.value) checkOut.value = minOut;
    }

    function ensureFirstRoom() {
        const existing = roomNodes()[0];
        if (existing) {
            normalizeDates(existing);
            return existing;
        }
        if (!roomItemsRoot || !roomItemTemplate) return null;
        const fragment = roomItemTemplate.content.cloneNode(true);
        roomItemsRoot.appendChild(fragment);
        const item = roomNodes()[0] || null;
        if (!item) return null;
        const select = item.querySelector('[data-field="roomType"]');
        const checkIn = item.querySelector('[data-field="checkIn"]');
        const checkOut = item.querySelector('[data-field="checkOut"]');
        if (select instanceof HTMLSelectElement && roomTypeIdInput?.value) select.value = roomTypeIdInput.value;
        if (checkIn instanceof HTMLInputElement) checkIn.value = checkInInput?.value || checkIn.value;
        if (checkOut instanceof HTMLInputElement) checkOut.value = checkOutInput?.value || checkOut.value;
        normalizeDates(item);
        return item;
    }

    function addRoomItem(seed) {
        if (!roomItemsRoot || !roomItemTemplate) return ensureFirstRoom();
        const fragment = roomItemTemplate.content.cloneNode(true);
        roomItemsRoot.appendChild(fragment);
        const nodes = roomNodes();
        const item = nodes[nodes.length - 1] || null;
        if (!item) return null;

        const select = item.querySelector('[data-field="roomType"]');
        const checkIn = item.querySelector('[data-field="checkIn"]');
        const checkOut = item.querySelector('[data-field="checkOut"]');
        const roomCount = item.querySelector('[data-field="roomCount"]');
        if (select instanceof HTMLSelectElement) {
            select.value = String(seed?.roomTypeId || roomTypeIdInput?.value || select.value || '');
        }
        if (checkIn instanceof HTMLInputElement) checkIn.value = seed?.checkIn || checkInInput?.value || checkIn.value;
        if (checkOut instanceof HTMLInputElement) checkOut.value = seed?.checkOut || checkOutInput?.value || checkOut.value;
        if (roomCount instanceof HTMLInputElement) roomCount.value = String(clampRoomCount(seed?.roomCount));
        normalizeDates(item);
        renumberRooms();
        syncHiddenFields();
        updateRoomButtons(seed?.roomTypeId);
        return item;
    }

    function setRoomSelection(roomId) {
        const nodes = roomNodes();
        let item;
        if (nodes.length === 0) {
            item = ensureFirstRoom();
        } else {
            item = nodes.find(node => node.classList.contains('is-active')) || nodes[0];
        }
        const select = item?.querySelector('[data-field="roomType"]');
        if (select instanceof HTMLSelectElement && roomId) {
            select.value = String(roomId);
        }
        if (item) {
            normalizeDates(item);
        }
        syncHiddenFields();
        renumberRooms();
        updateRoomButtons(roomId);

        const option = select?.selectedOptions?.[0];
        const roomName = option?.dataset?.roomName || option?.textContent?.trim() || '';
        const roomNameEl = document.getElementById('mobileBookingRoomName');
        if (roomNameEl && roomName) {
            roomNameEl.textContent = roomName;
        }

        document.dispatchEvent(new CustomEvent('otelturizm:room-selection-applied', {
            detail: { roomTypeId: roomId, roomName: roomName }
        }));
        openBookingArea();
    }

    function renumberRooms() {
        const nodes = roomNodes();
        nodes.forEach((node, index) => {
            const title = node.querySelector('[data-room-title]');
            if (title) title.textContent = 'Oda ' + (index + 1);
            node.setAttribute('data-room-index', String(index));
            const removeBtn = node.querySelector('[data-remove-room]');
            if (removeBtn) {
                removeBtn.hidden = nodes.length <= 1;
                removeBtn.textContent = 'Seçimi iptal et';
            }
        });
        const root = document.getElementById('multiRoomRoot');
        if (root) root.classList.toggle('has-multiple-rooms', nodes.length > 1);
    }

    function syncHiddenFields() {
        const nodes = roomNodes();
        nodes.forEach(normalizeDates);
        const items = nodes.map(readRoom).filter(x => x.roomTypeId > 0);
        const first = items[0];

        const totalRoomCount = items.reduce((sum, item) => sum + clampRoomCount(item.roomCount), 0);
        if (roomCountInput) roomCountInput.value = String(Math.max(1, totalRoomCount || items.length || nodes.length || 1));
        if (roomTypeIdInput && first) roomTypeIdInput.value = String(first.roomTypeId);
        if (checkInInput && first) checkInInput.value = first.checkIn;
        if (checkOutInput && first) checkOutInput.value = first.checkOut;
        if (roomsJsonInput) {
            roomsJsonInput.value = JSON.stringify(items.map(x => ({
                roomTypeId: x.roomTypeId,
                checkInDate: x.checkIn,
                checkOutDate: x.checkOut,
                roomCount: clampRoomCount(x.roomCount)
            })));
        }
        return items;
    }

    function getDetailI18n() {
        const root = document.querySelector('.otel-detail-template-v41');
        return {
            roomSelected: root?.dataset?.i18nRoomSelected || 'Seçili oda',
            roomReserve: root?.dataset?.i18nRoomReserve || 'Rezervasyon'
        };
    }

    function updateRoomButtons(activeRoomId) {
        const i18n = getDetailI18n();
        const selected = new Set(syncHiddenFields().map(x => String(x.roomTypeId)));
        document.querySelectorAll('.select-room-btn').forEach(btn => {
            const id = String(btn.getAttribute('data-room-id') || '');
            const isSelected = selected.has(id);
            btn.classList.toggle('is-selected-room', isSelected);
            if (!btn.dataset.originalText) {
                btn.dataset.originalText = btn.textContent?.trim() || i18n.roomReserve;
            }
            if (isSelected) {
                btn.innerHTML = '<span class="select-room-btn-text">' + i18n.roomSelected + '</span>';
                btn.dataset.lastPickedRoomIndex = '0';
            } else {
                btn.textContent = btn.dataset.originalText;
            }
        });
        if (activeRoomId) form.dataset.selectedRoomId = String(activeRoomId);
    }

    function isMobile() {
        return window.matchMedia && window.matchMedia('(max-width: 900px)').matches;
    }

    function openBookingArea() {
        if (isMobile() && bookingSidebar && bookingBackdrop) {
            bookingBackdrop.hidden = false;
            bookingSidebar.classList.add('is-open');
            bookingBackdrop.classList.add('is-open');
            document.documentElement.classList.add('modal-open');
            document.body.classList.add('modal-open');
        }
        const target = bookingSidebar || form;
        if (target instanceof HTMLElement) {
            const top = target.getBoundingClientRect().top + window.scrollY - 76;
            window.scrollTo({ top: Math.max(0, top), behavior: 'smooth' });
        }
    }

    function openModal(modal) {
        if (!modal || !actionBackdrop) return;
        modal.hidden = false;
        actionBackdrop.hidden = false;
        requestAnimationFrame(() => {
            modal.classList.add('is-open');
            actionBackdrop.classList.add('is-open');
            modal.setAttribute('aria-hidden', 'false');
        });
        document.body.style.overflow = 'hidden';
    }

    function closeModal(modal) {
        if (!modal || !actionBackdrop) return;
        modal.classList.remove('is-open');
        modal.setAttribute('aria-hidden', 'true');
        actionBackdrop.classList.remove('is-open');
        window.setTimeout(() => {
            modal.hidden = true;
            if (!document.querySelector('.detail-modal.is-open')) {
                actionBackdrop.hidden = true;
                document.body.style.overflow = '';
            }
        }, 120);
    }

    function showInfo(message) {
        if (!infoModal || !infoModalText || !infoModalActions) {
            window.alert(message);
            return;
        }
        if (infoModalTitle) infoModalTitle.textContent = 'Önce bir adımı tamamlayalım';
        infoModal.classList.remove('is-room-pick', 'is-warning', 'is-auth-required');
        infoModalText.textContent = message;
        infoModalActions.innerHTML = '<button type="button" class="detail-modal-primary" data-reservation-info-close>Tamam</button>';
        infoModalActions.querySelector('[data-reservation-info-close]')?.addEventListener('click', () => closeModal(infoModal));
        openModal(infoModal);
    }

    function validate(items) {
        ensurePaymentMethod();
        const adults = parseInt(adultCountInput?.value || '0', 10) || 0;
        if (!items.length || items.some(x => !x.roomTypeId || !x.checkIn || !x.checkOut || new Date(x.checkOut) <= new Date(x.checkIn))) {
            return 'Rezervasyon öncesi tarih, misafir ve oda bilgilerini kontrol edip tamamlayınız.';
        }
        if (adults < 1) return 'Rezervasyon için en az 1 yetişkin misafir seçiniz.';
        const payment = (paymentMethodSelect?.value || '').trim();
        if (payment !== 'Kapıda Ödeme' && payment !== 'Online Ödeme' && !payment.startsWith('Karma')) {
            return 'Lütfen ödeme planı seçiniz.';
        }
        return '';
    }

    function ensurePaymentMethod() {
        if (!paymentMethodSelect) return;
        const current = (paymentMethodSelect.value || '').trim();
        if (current) return;

        const fallback = Array.from(paymentMethodSelect.options)
            .find(x => x.value === 'Kapıda Ödeme')
            || Array.from(paymentMethodSelect.options).find(x => x.value);
        if (fallback) {
            paymentMethodSelect.value = fallback.value;
        }
    }

    function readConfirmI18n() {
        const el = document.getElementById('reservationConfirmI18nJson');
        if (!el) return {};
        try {
            return JSON.parse(el.textContent || '{}');
        } catch (err) {
            return {};
        }
    }

    const confirmI18n = readConfirmI18n();
    const confirmUiCulture = confirmI18n.uiCulture || 'tr-TR';

    function parseIsoDate(value) {
        if (!value || typeof value !== 'string') return null;
        const parts = value.split('-');
        if (parts.length !== 3) return null;
        const year = Number(parts[0]);
        const month = Number(parts[1]);
        const day = Number(parts[2]);
        if (!Number.isFinite(year) || !Number.isFinite(month) || !Number.isFinite(day)) return null;
        const parsed = new Date(year, month - 1, day);
        if (Number.isNaN(parsed.getTime())) return null;
        parsed.setHours(0, 0, 0, 0);
        return parsed;
    }

    function formatDateHuman(iso) {
        const parsed = parseIsoDate(iso);
        if (!parsed) return String(iso || '-');
        return parsed.toLocaleDateString(confirmUiCulture, {
            weekday: 'short',
            day: 'numeric',
            month: 'long',
            year: 'numeric'
        });
    }

    function countNights(checkIn, checkOut) {
        const start = parseIsoDate(checkIn);
        const end = parseIsoDate(checkOut);
        if (!start || !end) return 0;
        return Math.max(0, Math.round((end.getTime() - start.getTime()) / 86400000));
    }

    function formatConfirmTemplate(template, value) {
        return String(template || '{0}').replace('{0}', String(value));
    }

    function paymentConfirmIcon(paymentText) {
        const normalized = String(paymentText || '').toLowerCase();
        if (normalized.includes('online') || normalized.includes('kart')) return 'fa-credit-card';
        if (normalized.includes('karma')) return 'fa-wallet';
        return 'fa-hand-holding-dollar';
    }

    function buildConfirmRoomCard(item, index) {
        const nights = countNights(item.checkIn, item.checkOut);
        const nightsText = nights > 0 ? formatConfirmTemplate(confirmI18n.nights || '{0} gece', nights) : '';
        const roomLabel = formatConfirmTemplate(confirmI18n.room || 'Oda {0}', index + 1);
        const roomCount = clampRoomCount(item.roomCount);
        const roomCountText = roomCount > 1
            ? formatConfirmTemplate(confirmI18n.roomCount || '{0} oda', roomCount)
            : '';

        return '<article class="reservation-confirm-card reservation-confirm-card--room">' +
            '<div class="reservation-confirm-card__icon" aria-hidden="true"><i class="fas fa-bed"></i></div>' +
            '<div class="reservation-confirm-card__body">' +
            '<span class="reservation-confirm-card__label">' + escapeHtml(roomLabel) + '</span>' +
            '<strong class="reservation-confirm-card__title">' + escapeHtml(item.roomName) + '</strong>' +
            '<div class="reservation-confirm-card__dates">' +
            '<span class="reservation-confirm-date"><span class="reservation-confirm-date__tag">' + escapeHtml(confirmI18n.checkIn || 'Giriş') + '</span><time datetime="' + escapeHtml(item.checkIn || '') + '">' + escapeHtml(formatDateHuman(item.checkIn)) + '</time></span>' +
            '<span class="reservation-confirm-date-sep" aria-hidden="true"><i class="fas fa-arrow-right"></i></span>' +
            '<span class="reservation-confirm-date"><span class="reservation-confirm-date__tag">' + escapeHtml(confirmI18n.checkOut || 'Çıkış') + '</span><time datetime="' + escapeHtml(item.checkOut || '') + '">' + escapeHtml(formatDateHuman(item.checkOut)) + '</time></span>' +
            '</div>' +
            ((nightsText || roomCountText)
                ? '<div class="reservation-confirm-card__meta">' + escapeHtml([nightsText, roomCountText].filter(Boolean).join(' · ')) + '</div>'
                : '') +
            '</div></article>';
    }

    function buildConfirmPaymentCard(paymentText) {
        const icon = paymentConfirmIcon(paymentText);
        return '<article class="reservation-confirm-card reservation-confirm-card--payment">' +
            '<div class="reservation-confirm-card__icon" aria-hidden="true"><i class="fas ' + icon + '"></i></div>' +
            '<div class="reservation-confirm-card__body">' +
            '<span class="reservation-confirm-card__label">' + escapeHtml(confirmI18n.paymentPlan || 'Ödeme planı') + '</span>' +
            '<strong class="reservation-confirm-card__title">' + escapeHtml(paymentText) + '</strong>' +
            '<p class="reservation-confirm-card__note">' + escapeHtml(confirmI18n.paymentNote || '') + '</p>' +
            '</div></article>';
    }

    function updateConfirmTotalDisplay() {
        const totalRow = document.getElementById('reservationConfirmTotalRow');
        const totalEl = document.getElementById('reservationConfirmTotal');
        const bookingPrice = document.getElementById('bookingPrice');
        if (!totalRow || !totalEl) return;

        const raw = (bookingPrice?.textContent || '').replace(/[^\d]/g, '');
        const amount = Number(raw || 0);
        if (amount > 0) {
            totalEl.textContent = bookingPrice.textContent.trim();
            totalRow.hidden = false;
            return;
        }
        totalRow.hidden = true;
        totalEl.textContent = '—';
    }

    function buildConfirm(items) {
        if (!confirmSummary) return;
        const payment = (paymentMethodSelect?.value || '').trim() || (confirmI18n.notSelected || 'Seçilmedi');
        const roomCards = items.map((x, index) => buildConfirmRoomCard(x, index)).join('');
        confirmSummary.innerHTML =
            '<div class="reservation-confirm-stack">' + roomCards + buildConfirmPaymentCard(payment) + '</div>';
        updateConfirmTotalDisplay();
    }

    function escapeHtml(value) {
        return String(value || '').replace(/[&<>"']/g, ch => ({
            '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
        }[ch]));
    }

    document.addEventListener('click', function (event) {
        const target = event.target instanceof HTMLElement ? event.target : null;
        const quantityBtn = target?.closest?.('[data-room-quantity-action]');
        if (quantityBtn) {
            event.preventDefault();
            event.stopImmediatePropagation();
            const item = quantityBtn.closest('[data-room-item]');
            const input = item?.querySelector('[data-field="roomCount"]');
            if (input instanceof HTMLInputElement) {
                const delta = quantityBtn.getAttribute('data-room-quantity-action') === 'minus' ? -1 : 1;
                input.value = String(clampRoomCount(clampRoomCount(input.value) + delta));
                syncHiddenFields();
                updateRoomButtons();
            }
            return;
        }

        if (target?.closest?.('#reservationConfirmSubmitBtn')) {
            if (!pendingSubmit) return;
            event.preventDefault();
            event.stopImmediatePropagation();
            const items = syncHiddenFields();
            const error = validate(items);
            if (error) {
                pendingSubmit = false;
                closeModal(confirmModal);
                showInfo(error);
                return;
            }
            submitButton?.classList.add('is-submitting');
            form.dataset.reservationDirectSubmit = 'true';
            closeModal(confirmModal);
            HTMLFormElement.prototype.submit.call(form);
        }
    }, true);

    roomItemsRoot?.addEventListener('click', function (event) {
        const target = event.target instanceof HTMLElement ? event.target : null;
        const removeBtn = target?.closest?.('[data-remove-room]');
        if (!removeBtn) return;

        event.preventDefault();
        event.stopImmediatePropagation();

        const item = removeBtn.closest('[data-room-item]');
        const nodes = roomNodes();
        if (item && nodes.length > 1) {
            item.remove();
        }

        ensureFirstRoom();
        renumberRooms();
        syncHiddenFields();
        updateRoomButtons();
    }, true);

    roomItemsRoot?.addEventListener('change', function (event) {
        const target = event.target instanceof HTMLElement ? event.target : null;
        const item = target?.closest?.('[data-room-item]');
        if (!item) return;
        const quantity = item.querySelector('[data-field="roomCount"]');
        if (quantity instanceof HTMLInputElement) {
            quantity.value = String(clampRoomCount(quantity.value));
        }
        normalizeDates(item);
        syncHiddenFields();
        updateRoomButtons();
    }, true);

    addRoomButton?.addEventListener('click', function (event) {
        event.preventDefault();
        event.stopImmediatePropagation();
        const nodes = roomNodes();
        const last = nodes[nodes.length - 1] || null;
        const seed = last ? readRoom(last) : null;
        addRoomItem(seed || undefined);
        openBookingArea();
    }, true);

    form.addEventListener('submit', function (event) {
        if (form.dataset.reservationDirectSubmit === 'true') return;
        event.preventDefault();
        event.stopImmediatePropagation();

        const items = syncHiddenFields();
        const error = validate(items);
        if (error) {
            showInfo(error);
            return;
        }

        pendingSubmit = true;
        buildConfirm(items);
        openModal(confirmModal);
    }, true);

    ensureFirstRoom();
    ensurePaymentMethod();
    renumberRooms();
    syncHiddenFields();
    updateRoomButtons();

    const successRedirect = document.querySelector('[data-reservation-success-redirect]');
    if (successRedirect instanceof HTMLElement) {
        const url = successRedirect.getAttribute('data-reservation-success-redirect') || '/panel/user/rezervasyonlarim';
        window.setTimeout(() => {
            window.location.href = url;
        }, 1800);
    }
})();
