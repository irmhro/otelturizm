(function () {
    const form = document.querySelector("[data-sales-assistant-form]");
    const queryInput = document.getElementById("salesAssistantQuery");
    const dropdown = document.getElementById("salesAssistantDropdown");
    const scenarioWrap = document.querySelector("[data-sales-assistant-scenarios]");
    if (!form || !queryInput || !dropdown) {
        return;
    }

    const cityInput = form.querySelector('input[name="city"]');
    const districtInput = form.querySelector('input[name="district"]');
    const neighborhoodInput = form.querySelector('input[name="neighborhood"]');
    const minPriceInput = form.querySelector('input[name="minPrice"]');
    const maxPriceInput = form.querySelector('input[name="maxPrice"]');
    const minimumRatingInput = form.querySelector('input[name="minimumRating"]');
    const minimumReviewCountInput = form.querySelector('input[name="minimumReviewCount"]');
    const featureInput = form.querySelector('input[name="feature"]');
    const scenarioButtons = scenarioWrap ? Array.from(scenarioWrap.querySelectorAll(".sales-assistant-scenario")) : [];

    let debounceTimer = null;
    let activeRequestId = 0;
    let activeIndex = -1;
    let currentItems = [];
    let activeScenario = "";

    const hideDropdown = function () {
        dropdown.hidden = true;
        dropdown.innerHTML = "";
        activeIndex = -1;
        currentItems = [];
    };

    const createSelectionLink = function (hotelId) {
        const params = new URLSearchParams(window.location.search);
        const customerId = form.querySelector('input[name="customerId"]')?.value;
        params.set("hotelId", String(hotelId));
        if (customerId) {
            params.set("customerId", customerId);
        }
        params.set("searchTerm", queryInput.value || "");
        params.set("city", cityInput?.value || "");
        params.set("district", districtInput?.value || "");
        params.set("neighborhood", neighborhoodInput?.value || "");
        params.set("minPrice", minPriceInput?.value || "");
        params.set("maxPrice", maxPriceInput?.value || "");
        params.set("minimumRating", minimumRatingInput?.value || "");
        params.set("minimumReviewCount", minimumReviewCountInput?.value || "");
        params.set("feature", featureInput?.value || "");
        return "/panel/satis/yeni-rezervasyon?" + params.toString();
    };

    const createFilterLink = function (hotel) {
        const params = new URLSearchParams(window.location.search);
        params.delete("hotelId");
        params.set("searchTerm", hotel.hotelName || "");
        params.set("city", hotel.city || "");
        params.set("district", hotel.district || "");
        params.set("feature", Array.isArray(hotel.featureBadges) && hotel.featureBadges.length > 0 ? hotel.featureBadges[0] : "");
        params.set("minimumRating", hotel.ratingText || minimumRatingInput?.value || "");
        params.set("minimumReviewCount", minimumReviewCountInput?.value || "");
        params.set("minPrice", minPriceInput?.value || "");
        params.set("maxPrice", maxPriceInput?.value || "");
        params.set("neighborhood", neighborhoodInput?.value || "");
        return "/panel/satis/yeni-rezervasyon?" + params.toString();
    };

    const escapeHtml = function (value) {
        return (value || "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#39;");
    };

    const parsePrice = function (text) {
        const cleaned = (text || "").replace(/[^0-9.,]/g, "").replace(/\./g, "").replace(",", ".");
        const parsed = Number.parseFloat(cleaned);
        return Number.isFinite(parsed) ? parsed : Number.MAX_SAFE_INTEGER;
    };

    const applyScenario = function (hotels) {
        if (!Array.isArray(hotels)) {
            return [];
        }

        if (activeScenario === "cheap") {
            return hotels
                .slice()
                .sort(function (a, b) { return parsePrice(a.priceText) - parsePrice(b.priceText); })
                .slice(0, 5);
        }

        if (activeScenario === "rating4") {
            return hotels.filter(function (item) {
                const rating = Number.parseFloat((item.ratingText || "").replace(",", "."));
                return Number.isFinite(rating) && rating >= 4;
            });
        }

        if (activeScenario === "center") {
            return hotels.filter(function (item) {
                const haystack = ((item.address || "") + " " + (item.locationText || "") + " " + (item.district || "")).toLowerCase();
                return haystack.includes("merkez");
            });
        }

        return hotels;
    };

    const highlightItem = function (nextIndex) {
        const items = Array.from(dropdown.querySelectorAll(".sales-assistant-item"));
        if (items.length === 0) {
            activeIndex = -1;
            return;
        }

        activeIndex = Math.max(0, Math.min(nextIndex, items.length - 1));
        items.forEach(function (item, index) {
            item.classList.toggle("is-active", index === activeIndex);
            if (index === activeIndex) {
                item.scrollIntoView({ block: "nearest" });
            }
        });
    };

    const renderHotels = function (hotels) {
        const scenarioHotels = applyScenario(hotels);
        currentItems = scenarioHotels;
        if (!scenarioHotels || scenarioHotels.length === 0) {
            dropdown.hidden = false;
            dropdown.innerHTML = '<div class="sales-assistant-empty">Aramaya uygun otel bulunamadı.</div>';
            activeIndex = -1;
            return;
        }

        dropdown.hidden = false;
        dropdown.innerHTML = scenarioHotels.map(function (hotel, index) {
            const badges = Array.isArray(hotel.featureBadges)
                ? hotel.featureBadges.slice(0, 3).map(function (badge) {
                    return "<span>" + escapeHtml(badge) + "</span>";
                }).join("")
                : "";

            return '' +
                '<article class="sales-assistant-item" data-index="' + index + '" tabindex="-1">' +
                '<div>' +
                '<strong>' + escapeHtml(hotel.hotelName) + '</strong>' +
                '<small>' + escapeHtml(hotel.locationText) + " · " + escapeHtml(hotel.ratingText) + ' ★ · ' + escapeHtml(hotel.reviewCountText) + '</small>' +
                '<small>' + escapeHtml(hotel.address || "") + '</small>' +
                (badges ? '<div class="sales-assistant-badges">' + badges + '</div>' : '') +
                '</div>' +
                '<div class="sales-assistant-price">' +
                '<strong>' + escapeHtml(hotel.priceText) + '</strong>' +
                '<small>' + escapeHtml(hotel.todayDemandText || "") + '</small>' +
                '<div class="sales-assistant-actions">' +
                '<a class="sales-assistant-action is-primary" href="' + createSelectionLink(hotel.hotelId) + '">Hızlı Seç</a>' +
                '<a class="sales-assistant-action" href="' + createFilterLink(hotel) + '">Detaylı Filtreyi Uygula</a>' +
                '</div>' +
                '</div>' +
                '</article>';
        }).join("");

        highlightItem(0);
    };

    const runSearch = function () {
        const q = queryInput.value.trim();
        if (q.length < 2) {
            hideDropdown();
            return;
        }

        activeRequestId += 1;
        const requestId = activeRequestId;
        const params = new URLSearchParams({
            q: q,
            city: cityInput?.value || "",
            district: districtInput?.value || "",
            neighborhood: neighborhoodInput?.value || "",
            minPrice: minPriceInput?.value || "",
            maxPrice: maxPriceInput?.value || "",
            minimumRating: minimumRatingInput?.value || "",
            minimumReviewCount: minimumReviewCountInput?.value || "",
            feature: featureInput?.value || "",
            take: activeScenario === "cheap" ? "20" : "8"
        });

        fetch("/panel/satis/yeni-rezervasyon/otel-asistani?" + params.toString(), {
            method: "GET",
            headers: { "Accept": "application/json" }
        })
            .then(function (response) { return response.ok ? response.json() : null; })
            .then(function (payload) {
                if (requestId !== activeRequestId || !payload || payload.success !== true) {
                    return;
                }
                renderHotels(payload.hotels || []);
            })
            .catch(function () {
                if (requestId === activeRequestId) {
                    hideDropdown();
                }
            });
    };

    queryInput.addEventListener("input", function () {
        if (debounceTimer) {
            window.clearTimeout(debounceTimer);
        }
        debounceTimer = window.setTimeout(runSearch, 180);
    });

    queryInput.addEventListener("focus", function () {
        if (queryInput.value.trim().length >= 2) {
            runSearch();
        }
    });

    queryInput.addEventListener("keydown", function (event) {
        if (dropdown.hidden || currentItems.length === 0) {
            return;
        }

        if (event.key === "ArrowDown") {
            event.preventDefault();
            highlightItem(activeIndex + 1);
            return;
        }

        if (event.key === "ArrowUp") {
            event.preventDefault();
            highlightItem(activeIndex - 1);
            return;
        }

        if (event.key === "Enter") {
            event.preventDefault();
            const selected = currentItems[Math.max(0, activeIndex)];
            if (selected && selected.hotelId) {
                window.location.href = createSelectionLink(selected.hotelId);
            }
            return;
        }

        if (event.key === "Escape") {
            hideDropdown();
        }
    });

    dropdown.addEventListener("mousemove", function (event) {
        const target = event.target.closest(".sales-assistant-item");
        if (!target) {
            return;
        }
        const itemIndex = Number.parseInt(target.getAttribute("data-index") || "-1", 10);
        if (Number.isInteger(itemIndex) && itemIndex >= 0) {
            highlightItem(itemIndex);
        }
    });

    if (scenarioButtons.length > 0) {
        const setScenario = function (scenario) {
            activeScenario = scenario;
            scenarioButtons.forEach(function (button) {
                button.classList.toggle("is-active", button.getAttribute("data-scenario") === scenario);
            });
        };

        scenarioButtons.forEach(function (button) {
            button.addEventListener("click", function () {
                const scenario = button.getAttribute("data-scenario") || "";
                if (scenario === "reset") {
                    setScenario("");
                    if (minimumRatingInput) minimumRatingInput.value = "";
                    if (featureInput) featureInput.value = "";
                    if (queryInput.value.trim().length >= 2) {
                        runSearch();
                    } else {
                        hideDropdown();
                    }
                    return;
                }

                if (scenario === "rating4" && minimumRatingInput) {
                    minimumRatingInput.value = "4";
                }
                if (scenario === "center" && featureInput && !featureInput.value.trim()) {
                    featureInput.value = "Merkez";
                }

                setScenario(scenario);
                if (queryInput.value.trim().length < 2) {
                    queryInput.focus();
                    return;
                }
                runSearch();
            });
        });
    }

    document.addEventListener("click", function (event) {
        if (form.contains(event.target)) {
            return;
        }
        hideDropdown();
    });
})();
