(function () {
    'use strict';

    const cfg = window.__otelDetayConfig || {};

    // Backward-compat: eski view inline script global map'ler üretiyordu.
    // Artık config'ten besliyoruz.
    if (typeof window.__hotelRoomGalleries !== 'object' || window.__hotelRoomGalleries === null) {
        window.__hotelRoomGalleries = cfg.roomGalleries || {};
    }
    if (typeof window.__hotelRoomDetails !== 'object' || window.__hotelRoomDetails === null) {
        window.__hotelRoomDetails = cfg.roomDetails || {};
    }

    (function () {
        const galleryImages = Array.isArray(cfg.galleryImages) ? cfg.galleryImages : [];
        const mainImage = document.getElementById('mainGalleryImage');
        const mainImageWrap = document.querySelector('.gallery-main-image');
        const lightbox = document.getElementById('galleryLightbox');
        const lightboxImage = document.getElementById('galleryLightboxImage');
        const lightboxCaption = document.getElementById('galleryLightboxCaption');
        const lightboxThumbs = document.getElementById('galleryLightboxThumbs');
        const lightboxClose = document.getElementById('galleryLightboxClose');
        const lightboxPrev = document.getElementById('galleryLightboxPrev');
        const lightboxNext = document.getElementById('galleryLightboxNext');
        const lightboxFigure = lightbox?.querySelector('.gallery-lightbox-figure') || null;
        const saveButton = document.getElementById('saveHotelBtn');
        const bookingForm = document.getElementById('bookingForm');
        const paymentMethodSelect = document.getElementById('paymentMethodSelect');
        const roomsJsonInput = document.getElementById('roomsJsonInput');
        const roomItemsRoot = document.getElementById('roomItems');
        const roomItemTemplate = document.getElementById('roomItemTemplate');
        const addRoomBtn = document.getElementById('addRoomBtn');
        const roomTypeIdInput = document.getElementById('roomTypeIdInput');
        const checkInInput = document.getElementById('checkInDateInput');
        const checkOutInput = document.getElementById('checkOutDateInput');
        const bookingPrice = document.getElementById('bookingPrice');
        const breakdown = document.getElementById('priceBreakdown');
        const nightlyDetails = document.getElementById('nightlyBreakdownDetails');
        const nightlyList = document.getElementById('nightlyBreakdownList');
        const bookingQuoteNotice = document.getElementById('bookingQuoteNotice');
        const bookingPaymentNote = document.getElementById('bookingPaymentNote');
        const bookingPaymentMix = document.getElementById('bookingPaymentMix');
        let profileCompletionRequired = document.getElementById('profileCompletionRequired')?.value === 'true';
        const actionBackdrop = document.getElementById('detailActionBackdrop');
        const profileModal = document.getElementById('profileCompletionModal');
        const infoModal = document.getElementById('detailInfoModal');
        const infoModalTitle = document.getElementById('detailInfoModalTitle');
        const defaultInfoModalTitle = infoModalTitle?.textContent || 'Önce bir adımı tamamlayalım';
        const infoModalText = document.getElementById('detailInfoModalText');
        const infoModalActions = document.getElementById('detailInfoModalActions');
        const reservationConfirmModal = document.getElementById('reservationConfirmModal');
        const reservationConfirmSummary = document.getElementById('reservationConfirmSummary');
        const reservationConfirmSubmitBtn = document.getElementById('reservationConfirmSubmitBtn');
        const profileFeedback = document.getElementById('profileCompletionFeedback');
        const profileCompletionForm = document.getElementById('profileCompletionForm');
        const startConversationBtn = document.getElementById('startConversationBtn');
        const weatherPopup = document.getElementById('weatherPopup');
        const weatherPopupBackdrop = document.getElementById('weatherPopupBackdrop');
        const weatherPopupOpen = document.getElementById('weatherPopupOpen');
        const weatherPopupClose = document.getElementById('weatherPopupClose');
        const shareButton = document.getElementById('shareHotelBtn');
        const shareMenu = document.getElementById('shareMenu');
        const bookingAdvancedFields = document.getElementById('bookingAdvancedFields');
        const bookingSidebar = document.getElementById('bookingSidebar') || document.querySelector('.booking-sidebar');
        const bookingModalBackdrop = document.getElementById('bookingModalBackdrop');
        const bookingModalClose = document.getElementById('bookingModalClose');
        const openBookingSheetBtn = document.getElementById('openBookingSheetBtn');
        const mobileBookingPrice = document.getElementById('mobileBookingPrice');
        const activeDraftResumeLink = document.querySelector('.active-draft-link');
        const activeDraftMessage = document.querySelector('.active-draft-message');
        const openProfileCompletionFromBanner = document.getElementById('openProfileCompletionFromBanner');
        const openAmenitiesModal = document.getElementById('openAmenitiesModal');
        const amenitiesModal = document.getElementById('amenitiesModal');
        const cancelDraftForm = document.getElementById('cancelDraftForm');
        const activeDraftResumeUrl = String(cfg.activeDraftResumeUrl || '');
        const activeDraftPromptMessage = String(cfg.activeDraftPromptMessage || '');
        const shouldPromptActiveDraft = !!cfg.shouldPromptActiveDraft;
        const roomDetailModal = document.getElementById('roomDetailModal');
        const roomDetailModalTitle = document.getElementById('roomDetailModalTitle');
        const roomDetailModalRoomName = document.getElementById('roomDetailModalRoomName');
        const roomDetailModalSpecs = document.getElementById('roomDetailModalSpecs');
        const roomDetailModalDescription = document.getElementById('roomDetailModalDescription');
        const roomDetailModalCancellation = document.getElementById('roomDetailModalCancellation');
        const roomDetailModalGalleryBadge = document.getElementById('roomDetailModalGalleryBadge');
        const roomDetailModalFeatureBadge = document.getElementById('roomDetailModalFeatureBadge');
        const roomDetailModalImage = document.getElementById('roomDetailModalImage');
        const roomDetailModalThumbs = document.getElementById('roomDetailModalThumbs');
        const roomDetailModalGalleryOpen = document.getElementById('roomDetailModalGalleryOpen');
        const roomDetailModalThumbPrev = document.getElementById('roomDetailModalThumbPrev');
        const roomDetailModalThumbNext = document.getElementById('roomDetailModalThumbNext');
        const roomDetailModalBedType = document.getElementById('roomDetailModalBedType');
        const roomDetailModalSquareMeter = document.getElementById('roomDetailModalSquareMeter');
        const roomDetailModalCapacity = document.getElementById('roomDetailModalCapacity');
        const roomDetailModalPrice = document.getElementById('roomDetailModalPrice');
        const roomDetailModalSelectBtn = document.getElementById('roomDetailModalSelectBtn');
        const roomDetailModalGrid = document.getElementById('roomDetailModalGrid');
        const galleryMobileTrack = document.getElementById('galleryMobileTrack');
        const galleryMobileDots = Array.from(document.querySelectorAll('[data-gallery-dot-index]'));
        let adults = parseInt(document.getElementById('adultCountInput')?.value || '2', 10);
        let children = parseInt(document.getElementById('childCountInput')?.value || '0', 10);
        const roomCountInput = document.getElementById('roomCountInput');
        let latestQuoteRequest = null;
        let activeGalleryIndex = 0;
        let galleryScrollTicking = false;
        let activeRoomDetailGalleryItems = [];
        let activeRoomDetailName = 'Oda';
        let activeRoomDetailRoomId = '';
        let reservationSubmitConfirmed = false;
        let lightboxScrollY = 0;
        let lightboxPrevBodyStyles = null;

        function isMobileBookingMode() {
            return window.matchMedia && window.matchMedia('(max-width: 900px)').matches;
        }

        function openBookingSheet() {
            if (!bookingSidebar || !bookingModalBackdrop) {
                return;
            }

            bookingModalBackdrop.hidden = false;
            bookingSidebar.classList.add('is-open');
            bookingModalBackdrop.classList.add('is-open');
            document.documentElement.classList.add('modal-open');
            document.body.classList.add('modal-open');
        }

        function closeBookingSheet() {
            if (!bookingSidebar || !bookingModalBackdrop) {
                return;
            }

            bookingSidebar.classList.remove('is-open');
            bookingModalBackdrop.classList.remove('is-open');
            document.documentElement.classList.remove('modal-open');
            document.body.classList.remove('modal-open');
            window.setTimeout(function () {
                bookingModalBackdrop.hidden = true;
            }, 220);
        }

        // İlk yüklemede de seçili görselin blur arkaplanını set et
        if (mainImageWrap instanceof HTMLElement && mainImage instanceof HTMLImageElement && mainImage.src) {
            mainImageWrap.style.setProperty('--hotel-main-ambient', 'url(\"' + mainImage.src + '\")');
        }

        function syncGuestInputs() {
            document.getElementById('adultCount').textContent = adults;
            document.getElementById('adultCountInput').value = adults;
            document.getElementById('childCount').textContent = children;
            document.getElementById('childCountInput').value = children;
        }

        function getRoomItemNodes() {
            if (!roomItemsRoot) {
                return [];
            }
            return Array.from(roomItemsRoot.querySelectorAll('[data-room-item]'));
        }

        function getRoomItemData(item) {
            const roomTypeSelect = item?.querySelector('[data-field="roomType"]');
            const checkIn = item?.querySelector('[data-field="checkIn"]');
            const checkOut = item?.querySelector('[data-field="checkOut"]');
            const selectedOption = roomTypeSelect?.selectedOptions?.[0];
            const roomTypeId = parseInt(roomTypeSelect?.value || '0', 10) || 0;
            const roomName = selectedOption?.dataset.roomName || selectedOption?.textContent?.trim() || 'Seçilen oda';
            const maxGuests = Math.max(1, parseInt(selectedOption?.dataset.maxGuests || '1', 10) || 1);
            const maxAdults = Math.max(1, parseInt(selectedOption?.dataset.maxAdults || String(maxGuests), 10) || maxGuests);
            const maxChildren = Math.max(0, parseInt(selectedOption?.dataset.maxChildren || '0', 10) || 0);
            return {
                roomTypeId,
                roomName,
                checkIn: checkIn?.value || '',
                checkOut: checkOut?.value || '',
                roomCount: 1,
                capacity: { roomName, maxGuests, maxAdults, maxChildren }
            };
        }

        function syncLegacyHiddenInputsFromFirstItem() {
            const items = getRoomItemNodes();
            const first = items[0];
            if (!first) {
                return;
            }
            const data = getRoomItemData(first);
            if (roomTypeIdInput) {
                roomTypeIdInput.value = String(data.roomTypeId || 0);
            }
            if (checkInInput) {
                checkInInput.value = data.checkIn || '';
            }
            if (checkOutInput) {
                checkOutInput.value = data.checkOut || '';
            }
        }

        function getRoomCountValue() {
            return Math.max(1, parseInt(roomCountInput?.value || '1', 10) || 1);
        }

        function buildRoomCapacityMessage(capacity, totalAdults, totalChildren, roomCountValue, isMultiRoom) {
            const allowedGuestsTotal = capacity.maxGuests * roomCountValue;
            const allowedAdultsTotal = capacity.maxAdults * roomCountValue;
            const allowedChildrenTotal = capacity.maxChildren * roomCountValue;
            const totalGuests = totalAdults + totalChildren;

            if (totalAdults > allowedAdultsTotal) {
                if (isMultiRoom) {
                    const requiredRooms = Math.ceil(totalAdults / capacity.maxAdults);
                    const missing = Math.max(0, requiredRooms - roomCountValue);
                    return `Seçilen oda(lar) toplam yetişkin kapasitesi ${allowedAdultsTotal} kişi. ${totalAdults} yetişkin için en az ${requiredRooms} oda gerekir. ${missing > 0 ? `Lütfen ${missing} oda daha ekleyin.` : 'Lütfen oda seçiminizi kontrol edin.'}`;
                } else {
                    const requiredRooms = Math.ceil(totalAdults / capacity.maxAdults);
                    return `${capacity.roomName} odası kişi kapasitesi: en fazla ${capacity.maxAdults} yetişkin. ${totalAdults} yetişkin için en az ${requiredRooms} oda kiralamanız gerekmektedir.`;
                }
            }

            if (totalChildren > allowedChildrenTotal) {
                if (isMultiRoom) {
                    const requiredRooms = capacity.maxChildren > 0
                        ? Math.ceil(totalChildren / capacity.maxChildren)
                        : roomCountValue + 1;
                    const missing = Math.max(0, requiredRooms - roomCountValue);
                    return `Seçilen oda(lar) toplam çocuk kapasitesi ${allowedChildrenTotal} kişi. ${totalChildren} çocuk için en az ${requiredRooms} oda gerekir. ${missing > 0 ? `Lütfen ${missing} oda daha ekleyin.` : 'Lütfen oda seçiminizi kontrol edin.'}`;
                } else {
                    const requiredRooms = capacity.maxChildren > 0
                        ? Math.ceil(totalChildren / capacity.maxChildren)
                        : roomCountValue + 1;
                    return `${capacity.roomName} odası kişi kapasitesi: en fazla ${capacity.maxChildren} çocuk. ${totalChildren} çocuk için en az ${requiredRooms} oda kiralamanız gerekmektedir.`;
                }
            }

            if (totalGuests > allowedGuestsTotal) {
                if (isMultiRoom) {
                    const requiredRooms = Math.ceil(totalGuests / capacity.maxGuests);
                    const missing = Math.max(0, requiredRooms - roomCountValue);
                    return `Seçilen oda(lar) toplam kapasitesi ${allowedGuestsTotal} kişi. Toplam ${totalGuests} misafir için en az ${requiredRooms} oda gerekir. ${missing > 0 ? `Lütfen ${missing} oda daha ekleyin.` : 'Lütfen oda seçiminizi kontrol edin.'}`;
                } else {
                    const requiredRooms = Math.ceil(totalGuests / capacity.maxGuests);
                    return `${capacity.roomName} odası ${capacity.maxGuests} kişiliktir. Toplam ${totalGuests} misafir için en az ${requiredRooms} oda kiralamanız gerekmektedir.`;
                }
            }

            return null;
        }

        function ensureGuestCapacity(showWarning) {
            const items = getRoomItemNodes();
            if (!items.length) {
                return true;
            }

            const roomCountValue = Math.max(1, items.length);
            const totalCapacity = items
                .map(getRoomItemData)
                .reduce(function (acc, item) {
                    acc.maxGuests += item.capacity.maxGuests;
                    acc.maxAdults += item.capacity.maxAdults;
                    acc.maxChildren += item.capacity.maxChildren;
                    acc.roomName = item.capacity.roomName;
                    return acc;
                }, { roomName: 'Seçilen oda', maxGuests: 0, maxAdults: 0, maxChildren: 0 });

            const isMulti = items.length > 1;
            const roomCapacityMessage = buildRoomCapacityMessage(totalCapacity, adults, children, roomCountValue, isMulti);
            if (!roomCapacityMessage) {
                return true;
            }

            if (showWarning) {
                showInfoModal(roomCapacityMessage, false);
            }

            return false;
        }

        function setActiveMobileDot(index) {
            if (!galleryMobileDots.length) {
                return;
            }

            galleryMobileDots.forEach(function (dot) {
                const dotIndex = parseInt(dot.getAttribute('data-gallery-dot-index') || '0', 10);
                const isActive = dotIndex === index;
                dot.classList.toggle('active', isActive);
                dot.setAttribute('aria-selected', isActive ? 'true' : 'false');
            });
        }

        function scrollMobileTrackTo(index) {
            if (!galleryMobileTrack) {
                return;
            }

            const slide = galleryMobileTrack.querySelector('[data-gallery-index="' + index + '"]');
            if (!slide) {
                return;
            }

            slide.scrollIntoView({ behavior: 'smooth', inline: 'center', block: 'nearest' });
        }

        function syncMainImage(index) {
            if (!Array.isArray(galleryImages) || !galleryImages.length || !mainImage) {
                return;
            }

            const safeIndex = Math.min(Math.max(index, 0), galleryImages.length - 1);
            activeGalleryIndex = safeIndex;
            mainImage.src = galleryImages[safeIndex];
            if (mainImageWrap instanceof HTMLElement) {
                mainImageWrap.style.setProperty('--hotel-main-ambient', 'url(\"' + (galleryImages[safeIndex] || '') + '\")');
            }
            document.querySelectorAll('.gallery-thumb, .gallery-filmstrip-thumb').forEach(function (thumb) {
                const thumbIndex = parseInt(thumb.getAttribute('data-gallery-index') || '0', 10);
                thumb.classList.toggle('active', thumbIndex === safeIndex);
            });
            setActiveMobileDot(safeIndex);
        }

        function syncLightboxThumbs() {
            if (!lightboxThumbs || !Array.isArray(galleryImages) || !galleryImages.length) {
                return;
            }

            lightboxThumbs.innerHTML = galleryImages.map(function (src, idx) {
                const isActive = idx === activeGalleryIndex;
                return '<button type="button" class="gallery-lightbox-thumb' + (isActive ? ' active' : '') + '" data-lightbox-thumb-index="' + idx + '" aria-label="Gorsel ' + (idx + 1) + '">' +
                    '<img src="' + src + '" alt="Kucuk gorsel ' + (idx + 1) + '" />' +
                    '</button>';
            }).join('');

            lightboxThumbs.querySelectorAll('[data-lightbox-thumb-index]').forEach(function (btn) {
                btn.addEventListener('click', function () {
                    const idx = parseInt(btn.getAttribute('data-lightbox-thumb-index') || '0', 10);
                    activeGalleryIndex = Math.min(Math.max(idx, 0), galleryImages.length - 1);
                    if (lightboxImage) {
                        lightboxImage.src = galleryImages[activeGalleryIndex];
                    }
                    if (lightboxCaption) {
                        lightboxCaption.textContent = (activeGalleryIndex + 1) + ' / ' + galleryImages.length;
                    }
                    syncMainImage(activeGalleryIndex);
                    scrollMobileTrackTo(activeGalleryIndex);
                    setHotelLightboxAmbientSources();
                    syncLightboxThumbs();
                });
            });

            // aktif thumb görünür kalsın
            const active = lightboxThumbs.querySelector('.gallery-lightbox-thumb.active');
            active?.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });
        }

        function setHotelLightboxAmbientSources() {
            if (!(lightboxFigure instanceof HTMLElement) || !Array.isArray(galleryImages) || galleryImages.length === 0) {
                return;
            }

            const prevIndex = (activeGalleryIndex - 1 + galleryImages.length) % galleryImages.length;
            const nextIndex = (activeGalleryIndex + 1) % galleryImages.length;
            lightboxFigure.style.setProperty('--hotel-gallery-prev', 'url(\"' + (galleryImages[prevIndex] || '') + '\")');
            lightboxFigure.style.setProperty('--hotel-gallery-next', 'url(\"' + (galleryImages[nextIndex] || '') + '\")');

            if (lightbox instanceof HTMLElement) {
                lightbox.style.setProperty('--hotel-gallery-active', 'url(\"' + (galleryImages[activeGalleryIndex] || '') + '\")');
            }
        }

        function openLightbox(index) {
            if (!lightbox || !lightboxImage || !Array.isArray(galleryImages) || !galleryImages.length) {
                return;
            }

            syncMainImage(index);
            lightboxImage.src = galleryImages[activeGalleryIndex];
            if (lightboxCaption) {
                lightboxCaption.textContent = (activeGalleryIndex + 1) + ' / ' + galleryImages.length;
            }
            lightbox.hidden = false;
            // Scroll kilidi + scrollbar zıplamasını engelle
            lightboxScrollY = window.scrollY || 0;
            const docEl = document.documentElement;
            const sbw = Math.max(0, (window.innerWidth || 0) - (docEl?.clientWidth || 0));
            lightboxPrevBodyStyles = {
                overflow: document.body.style.overflow || '',
                position: document.body.style.position || '',
                top: document.body.style.top || '',
                left: document.body.style.left || '',
                right: document.body.style.right || '',
                width: document.body.style.width || '',
                paddingRight: document.body.style.paddingRight || '',
                docOverflow: docEl?.style.overflow || ''
            };
            if (docEl) docEl.style.overflow = 'hidden';
            document.body.style.overflow = 'hidden';
            document.body.style.position = 'fixed';
            document.body.style.top = (-lightboxScrollY) + 'px';
            document.body.style.left = '0';
            document.body.style.right = '0';
            document.body.style.width = '100%';
            if (sbw > 0) {
                document.body.style.paddingRight = sbw + 'px';
            }
            setHotelLightboxAmbientSources();
            syncLightboxThumbs();
        }
    })();

    (function () {
        const modal = document.getElementById('roomGalleryModal');
        const mainImage = document.getElementById('roomGalleryMainImage');
        const title = document.getElementById('roomGalleryTitle');
        const thumbs = document.getElementById('roomGalleryThumbs');
        const dialog = modal?.querySelector('.room-gallery-dialog') || null;
        const prevButton = document.getElementById('roomGalleryPrev');
        const nextButton = document.getElementById('roomGalleryNext');
        const triggers = document.querySelectorAll('.room-gallery-trigger');
        let galleryItems = [];
        let currentIndex = 0;
        let currentRoomName = '';

        function setAmbientSources() {
            if (!(dialog instanceof HTMLElement) || !Array.isArray(galleryItems) || galleryItems.length === 0) {
                return;
            }

            const prevIndex = (currentIndex - 1 + galleryItems.length) % galleryItems.length;
            const nextIndex = (currentIndex + 1) % galleryItems.length;
            dialog.style.setProperty('--room-gallery-prev', 'url(\"' + (galleryItems[prevIndex] || '') + '\")');
            dialog.style.setProperty('--room-gallery-next', 'url(\"' + (galleryItems[nextIndex] || '') + '\")');
        }

        function renderGallery() {
            if (!mainImage || !title || !thumbs || galleryItems.length === 0) {
                return;
            }

            mainImage.src = galleryItems[currentIndex];
            mainImage.alt = currentRoomName + ' gorsel ' + (currentIndex + 1);
            title.textContent = currentRoomName + ' · ' + (currentIndex + 1) + '/' + galleryItems.length;
            thumbs.innerHTML = galleryItems.map(function (item, index) {
                const isActive = index === currentIndex ? 'active' : '';
                return '<button type="button" class="room-gallery-thumb ' + isActive + '" data-room-thumb-index="' + index + '">' +
                    '<img src="' + item + '" alt="' + currentRoomName + ' kucuk gorsel ' + (index + 1) + '">' +
                    '</button>';
            }).join('');

            thumbs.querySelectorAll('[data-room-thumb-index]').forEach(function (button) {
                button.addEventListener('click', function () {
                    currentIndex = Number(button.getAttribute('data-room-thumb-index') || '0');
                    renderGallery();
                });
            });

            setAmbientSources();
        }

        function openGallery(items, roomName, startIndex) {
            if (!modal || !items || items.length === 0) {
                return;
            }

            galleryItems = items;
            currentRoomName = roomName || 'Oda';
            currentIndex = Math.max(0, Math.min(startIndex || 0, items.length - 1));
            renderGallery();
            modal.hidden = false;
            document.body.classList.add('room-gallery-open');
        }

        function closeGallery() {
            if (!modal) {
                return;
            }

            modal.hidden = true;
            document.body.classList.remove('room-gallery-open');
        }

        // OtelDetay ana inline script'i (henüz taşınmadı) bu fonksiyonu doğrudan çağırıyor.
        // Bu yüzden global alias veriyoruz (tam taşıma tamamlanınca kaldırılabilir).
        if (typeof window.openGallery !== 'function') {
            window.openGallery = openGallery;
        }
        if (typeof window.closeGallery !== 'function') {
            window.closeGallery = closeGallery;
        }

        triggers.forEach(function (trigger) {
            trigger.addEventListener('click', function () {
                const id = Number(trigger.getAttribute('data-room-id') || '0');
                const startIndex = Number(trigger.getAttribute('data-room-gallery-start') || '0');
                try {
                    const map = window.__hotelRoomGalleries || {};
                    const items = (id && map && map[id]) ? map[id] : [];
                    openGallery(Array.isArray(items) ? items : [], trigger.getAttribute('data-room-name') || 'Oda', startIndex);
                } catch (_error) {
                    openGallery([], trigger.getAttribute('data-room-name') || 'Oda', startIndex);
                }
            });
        });

        document.querySelectorAll('[data-room-gallery-close]').forEach(function (button) {
            button.addEventListener('click', closeGallery);
        });

        prevButton?.addEventListener('click', function () {
            if (galleryItems.length === 0) {
                return;
            }

            currentIndex = (currentIndex - 1 + galleryItems.length) % galleryItems.length;
            renderGallery();
        });

        nextButton?.addEventListener('click', function () {
            if (galleryItems.length === 0) {
                return;
            }

            currentIndex = (currentIndex + 1) % galleryItems.length;
            renderGallery();
        });

        document.addEventListener('keydown', function (event) {
            if (!modal || modal.hidden) {
                return;
            }

            if (event.key === 'Escape') {
                closeGallery();
            } else if (event.key === 'ArrowLeft') {
                prevButton?.click();
            } else if (event.key === 'ArrowRight') {
                nextButton?.click();
            }
        });
    })();

    (function () {
        const modal = document.getElementById('discountModal');
        const title = document.getElementById('discountModalTitle');
        const desc = document.getElementById('discountModalDesc');
        const visual = document.getElementById('discountModalVisual');
        const image = document.getElementById('discountModalImage');
        if (!modal || !title || !desc || !visual || !image) return;

        function openDiscount(data) {
            title.textContent = data.title || 'İndirim';
            desc.textContent = data.desc || '';
            const img = (data.image || '').trim();
            if (img) {
                image.src = img;
                visual.hidden = false;
            } else {
                image.src = '';
                visual.hidden = true;
            }
            modal.hidden = false;
            document.body.classList.add('discount-open');
        }

        function closeDiscount() {
            modal.hidden = true;
            document.body.classList.remove('discount-open');
        }

        document.querySelectorAll('[data-discount-open]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                openDiscount({
                    title: btn.getAttribute('data-discount-title') || '',
                    desc: btn.getAttribute('data-discount-desc') || '',
                    image: btn.getAttribute('data-discount-image') || ''
                });
            });
        });

        document.querySelectorAll('[data-discount-close]').forEach(function (btn) {
            btn.addEventListener('click', closeDiscount);
        });

        document.addEventListener('keydown', function (event) {
            if (!modal || modal.hidden) {
                return;
            }
            if (event.key === 'Escape') {
                closeDiscount();
            }
        });
    })();
})();

