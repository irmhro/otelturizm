(function () {
    const wrappers = document.querySelectorAll("[data-header-bildiri]");
    if (!wrappers.length) {
        return;
    }

    const mobileQuery = window.matchMedia("(max-width: 991px)");

    const closeAll = function () {
        wrappers.forEach(function (wrapper) {
            wrapper.dataset.open = "false";
            const toggle = wrapper.querySelector("[data-header-bildiri-toggle]");
            const dropdown = wrapper.querySelector("[data-header-bildiri-dropdown]");
            if (toggle) {
                toggle.setAttribute("aria-expanded", "false");
            }
            if (dropdown instanceof HTMLElement) {
                dropdown.classList.remove("is-fixed");
                dropdown.style.position = "";
                dropdown.style.top = "";
                dropdown.style.left = "";
                dropdown.style.right = "";
                dropdown.style.width = "";
                dropdown.style.maxWidth = "";
            }
        });
    };

    const positionDropdown = function (wrapper) {
        const dropdown = wrapper.querySelector("[data-header-bildiri-dropdown]");
        const toggle = wrapper.querySelector("[data-header-bildiri-toggle]");
        if (!(dropdown instanceof HTMLElement) || !(toggle instanceof HTMLElement)) {
            return;
        }

        if (!mobileQuery.matches) {
            dropdown.classList.remove("is-fixed");
            dropdown.style.position = "";
            dropdown.style.top = "";
            dropdown.style.left = "";
            dropdown.style.right = "";
            dropdown.style.width = "";
            dropdown.style.maxWidth = "";
            return;
        }

        const toggleRect = toggle.getBoundingClientRect();

        dropdown.classList.add("is-fixed");
        dropdown.style.position = "fixed";
        dropdown.style.top = `${Math.round(toggleRect.bottom + 10)}px`;
        dropdown.style.left = "";
        dropdown.style.right = "";
        dropdown.style.width = "";
        dropdown.style.maxWidth = "";
    };

    const getAntiForgeryToken = function (wrapper) {
        const antiForgery = wrapper.querySelector("input[name='__RequestVerificationToken']");
        return antiForgery instanceof HTMLInputElement && antiForgery.value ? antiForgery.value : "";
    };

    const markItemsRead = async function (wrapper, itemKeys) {
        const keys = Array.isArray(itemKeys)
            ? itemKeys.filter(Boolean)
            : Array.from(wrapper.querySelectorAll("[data-header-item-key][data-header-item-read='0']"))
                .map((item) => item.getAttribute("data-header-item-key") || "")
                .filter(Boolean);

        if (!keys.length) {
            return;
        }

        const panelKey = wrapper.getAttribute("data-panel-key");
        if (!panelKey) {
            return;
        }

        const token = getAntiForgeryToken(wrapper);
        if (!token) {
            return;
        }

        try {
            const response = await fetch("/panel/header-bildiri/okundu", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": token
                },
                credentials: "same-origin",
                body: JSON.stringify({ panelKey: panelKey, itemKeys: keys })
            });

            if (!response.ok) {
                return;
            }

            keys.forEach((key) => {
                const item = wrapper.querySelector(`[data-header-item-key="${CSS.escape(key)}"]`);
                if (item instanceof HTMLElement) {
                    item.setAttribute("data-header-item-read", "1");
                    item.classList.remove("is-unread");
                    item.classList.add("is-read");
                }
            });

            await refreshSummary(wrapper);
        } catch (_error) {
            // UI akisini bozma.
        }
    };

    const hasUnread = wrappers.length > 0 && Array.from(wrappers).some((wrapper) => {
        const unread = Number(wrapper.getAttribute("data-unread-count") || "0");
        return unread > 0;
    });

    let audioPermissionRequested = false;
    let browserPermissionRequested = false;

    const updateUnreadBadge = function (wrapper, unreadCount) {
        const count = Math.max(0, Number(unreadCount || 0));
        wrapper.setAttribute("data-unread-count", String(count));

        const toggle = wrapper.querySelector("[data-header-bildiri-toggle]");
        if (!(toggle instanceof HTMLElement)) {
            return;
        }

        let badge = toggle.querySelector(".header-bildiri-count");
        if (count > 0) {
            if (!badge) {
                badge = document.createElement("span");
                badge.className = "header-bildiri-count";
                toggle.appendChild(badge);
            }
            badge.textContent = String(count);
        } else if (badge) {
            badge.remove();
        }

        const headUnread = wrapper.querySelector("[data-header-bildiri-unread-label]");
        if (headUnread) {
            headUnread.textContent = `${count} okunmamis`;
        }
    };

    const renderSummaryItems = function (wrapper, items) {
        const list = wrapper.querySelector("[data-header-bildiri-list]");
        if (!(list instanceof HTMLElement)) {
            return;
        }

        list.innerHTML = "";
        const safeItems = Array.isArray(items) ? items : [];
        if (!safeItems.length) {
            const placeholder = document.createElement("li");
            placeholder.className = "header-bildiri-item tone-info is-read";
            placeholder.innerHTML = `
                <a href="#" data-header-item-link>
                    <i class="fas fa-sparkles"></i>
                    <div class="header-bildiri-content">
                        <strong>Paneliniz hazir</strong>
                        <p>Yeni bildirim olustugunda bu alanda otomatik gosterilir.</p>
                        <div class="header-bildiri-time">
                            <span class="header-bildiri-time-chip">Bugun</span>
                        </div>
                    </div>
                </a>`;
            list.appendChild(placeholder);
            return;
        }

        safeItems.forEach((item) => {
            const li = document.createElement("li");
            li.className = `header-bildiri-item tone-${item.tone || "info"} ${item.isRead ? "is-read" : "is-unread"}`;
            li.setAttribute("data-header-item-key", item.itemKey || "");
            li.setAttribute("data-header-item-read", item.isRead ? "1" : "0");
            li.innerHTML = `
                <a href="${item.url || "#"}" data-header-item-link>
                    <i class="fas ${item.iconClass || "fa-bell"}"></i>
                    <div class="header-bildiri-content">
                        <strong>${item.title || ""}</strong>
                        <p>${item.description || ""}</p>
                        <div class="header-bildiri-time">
                            <span class="header-bildiri-time-chip">${item.timeLabel || ""}</span>
                            <span class="header-bildiri-time-chip">${item.absoluteTimeLabel || ""}</span>
                        </div>
                    </div>
                </a>`;
            list.appendChild(li);
            const link = li.querySelector("[data-header-item-link]");
            if (link instanceof HTMLAnchorElement) {
                link.addEventListener("click", () => {
                    markItemsRead(wrapper, [item.itemKey].filter(Boolean));
                });
            }
        });
    };

    const refreshSummary = async function (wrapper) {
        const panelKey = wrapper.getAttribute("data-panel-key");
        if (!panelKey) {
            return null;
        }

        const summary = await fetchSummary(panelKey);
        if (!summary) {
            return null;
        }

        updateUnreadBadge(wrapper, summary.unreadCount || 0);
        renderSummaryItems(wrapper, summary.items || []);

        const viewAll = wrapper.querySelector("[data-header-bildiri-view-all]");
        if (viewAll instanceof HTMLAnchorElement) {
            if (summary.hasMoreItems && summary.inboxUrl) {
                viewAll.style.display = "";
                viewAll.href = summary.inboxUrl;
                viewAll.textContent = `Tüm bildirimler (${summary.allItemsCount || summary.totalCount || 0})`;
            } else {
                viewAll.style.display = "none";
            }
        }

        const clearButton = wrapper.querySelector("[data-header-bildiri-clear]");
        if (clearButton instanceof HTMLButtonElement) {
            clearButton.disabled = !(summary.totalCount > 0);
        }

        return summary;
    };

    const clearAllNotifications = async function (wrapper) {
        const panelKey = wrapper.getAttribute("data-panel-key");
        const token = getAntiForgeryToken(wrapper);
        if (!panelKey || !token) {
            return;
        }

        const clearButton = wrapper.querySelector("[data-header-bildiri-clear]");
        if (clearButton instanceof HTMLButtonElement) {
            clearButton.disabled = true;
        }

        try {
            const response = await fetch("/panel/header-bildiri/temizle", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": token
                },
                credentials: "same-origin",
                body: JSON.stringify({ panelKey: panelKey })
            });

            if (!response.ok) {
                if (clearButton instanceof HTMLButtonElement) {
                    clearButton.disabled = false;
                }
                return;
            }

            updateUnreadBadge(wrapper, 0);
            await refreshSummary(wrapper);
        } catch (_error) {
            if (clearButton instanceof HTMLButtonElement) {
                clearButton.disabled = false;
            }
        }
    };

    const requestBrowserPermission = function () {
        if (browserPermissionRequested || typeof Notification === "undefined") {
            return;
        }
        browserPermissionRequested = true;

        const askPermission = function () {
            if (typeof Notification === "undefined" || Notification.permission !== "default") {
                return;
            }
            Notification.requestPermission().catch(() => {
                // Tarayici istegi reddederse sessizce gec.
            });
        };

        const trigger = function () {
            askPermission();
            document.removeEventListener("click", trigger);
            document.removeEventListener("keydown", trigger);
        };

        document.addEventListener("click", trigger, { once: true });
        document.addEventListener("keydown", trigger, { once: true });
    };

    const showDesktopNotification = function (panelLabel, unreadCount, items) {
        if (typeof Notification === "undefined" || Notification.permission !== "granted") {
            return;
        }

        const firstRealItem = Array.isArray(items)
            ? items.find((item) => item && !item.isPlaceholder)
            : null;
        const body = firstRealItem
            ? `${firstRealItem.title}\n${firstRealItem.absoluteTimeLabel || firstRealItem.timeLabel || ""}`
            : `${unreadCount} okunmamis bildirim var.`;
        const notification = new Notification(`${panelLabel || "Panel"} bildirimleri`, {
            body: body,
            tag: `panel-header-bildiri-${panelLabel || "panel"}`,
            renotify: true
        });

        if (firstRealItem && firstRealItem.url) {
            notification.onclick = function () {
                window.location.href = firstRealItem.url;
                notification.close();
            };
        }
    };

    const playAlertSound = function () {
        const audio = document.getElementById("partnerNotificationAudio");
        if (!(audio instanceof HTMLAudioElement)) {
            return;
        }

        const tryPlay = function () {
            audio.currentTime = 0;
            const playPromise = audio.play();
            if (playPromise && typeof playPromise.catch === "function") {
                playPromise.catch(() => {
                    // Tarayici izin vermediyse sessizce gec.
                });
            }
        };

        if (!audioPermissionRequested) {
            audioPermissionRequested = true;
            const unlock = function () {
                tryPlay();
                document.removeEventListener("click", unlock);
                document.removeEventListener("keydown", unlock);
            };
            document.addEventListener("click", unlock, { once: true });
            document.addEventListener("keydown", unlock, { once: true });
        } else {
            tryPlay();
        }
    };

    const fetchSummary = async function (panelKey) {
        if (!panelKey) {
            return null;
        }

        const response = await fetch(`/panel/header-bildiri/ozet?panelKey=${encodeURIComponent(panelKey)}`, {
            method: "GET",
            credentials: "same-origin",
            cache: "no-store"
        });
        if (!response.ok) {
            return null;
        }

        return response.json();
    };

    const startUnreadPolling = function () {
        window.setInterval(async function () {
            for (const wrapper of wrappers) {
                const panelKey = wrapper.getAttribute("data-panel-key");
                if (!panelKey) {
                    continue;
                }

                try {
                    const previousUnread = Number(wrapper.getAttribute("data-unread-count") || "0");
                    const summary = await fetchSummary(panelKey);
                    if (!summary) {
                        continue;
                    }

                    const nextUnread = Number(summary.unreadCount || 0);
                    updateUnreadBadge(wrapper, nextUnread);

                    if (nextUnread > previousUnread) {
                        playAlertSound();
                        showDesktopNotification(summary.panelLabel || "Panel", nextUnread, summary.items || []);
                    }
                } catch (_error) {
                    // Polling hatasinda UI akisini bozma.
                }
            }
        }, 60 * 1000);
    };

    if (hasUnread) {
        playAlertSound();
        requestBrowserPermission();
    }
    startUnreadPolling();

    window.addEventListener("resize", function () {
        wrappers.forEach(function (wrapper) {
            if (wrapper.dataset.open === "true") {
                positionDropdown(wrapper);
            }
        });
    });

    mobileQuery.addEventListener("change", function () {
        wrappers.forEach(function (wrapper) {
            if (wrapper.dataset.open === "true") {
                positionDropdown(wrapper);
            } else {
                const dropdown = wrapper.querySelector("[data-header-bildiri-dropdown]");
                if (dropdown instanceof HTMLElement) {
                    dropdown.classList.remove("is-fixed");
                    dropdown.style.position = "";
                    dropdown.style.top = "";
                    dropdown.style.left = "";
                    dropdown.style.right = "";
                    dropdown.style.width = "";
                    dropdown.style.maxWidth = "";
                }
            }
        });
    });

    wrappers.forEach(function (wrapper) {
        const toggle = wrapper.querySelector("[data-header-bildiri-toggle]");
        if (!toggle) {
            return;
        }

        toggle.addEventListener("click", function (event) {
            event.preventDefault();
            const isOpen = wrapper.dataset.open === "true";
            closeAll();
            wrapper.dataset.open = isOpen ? "false" : "true";
            toggle.setAttribute("aria-expanded", isOpen ? "false" : "true");
            if (!isOpen) {
                requestBrowserPermission();
                positionDropdown(wrapper);
                markItemsRead(wrapper);
            }
        });

        wrapper.querySelectorAll("[data-header-item-link]").forEach((link) => {
            link.addEventListener("click", () => {
                markItemsRead(wrapper);
            });
        });

        const clearButton = wrapper.querySelector("[data-header-bildiri-clear]");
        if (clearButton instanceof HTMLButtonElement) {
            clearButton.addEventListener("click", function (event) {
                event.preventDefault();
                event.stopPropagation();
                clearAllNotifications(wrapper);
            });
        }
    });

    document.addEventListener("click", function (event) {
        const target = event.target;
        if (!(target instanceof Element)) {
            closeAll();
            return;
        }

        if (!target.closest("[data-header-bildiri]")) {
            closeAll();
        }
    });
})();
