(function () {
    const wrappers = document.querySelectorAll("[data-header-bildiri]");
    if (!wrappers.length) {
        return;
    }

    const closeAll = function () {
        wrappers.forEach(function (wrapper) {
            wrapper.dataset.open = "false";
            const toggle = wrapper.querySelector("[data-header-bildiri-toggle]");
            if (toggle) {
                toggle.setAttribute("aria-expanded", "false");
            }
        });
    };

    const markItemsRead = async function (wrapper) {
        const unreadItems = Array.from(wrapper.querySelectorAll("[data-header-item-key][data-header-item-read='0']"));
        if (!unreadItems.length) {
            return;
        }

        const panelKey = wrapper.getAttribute("data-panel-key");
        if (!panelKey) {
            return;
        }

        const itemKeys = unreadItems
            .map((item) => item.getAttribute("data-header-item-key") || "")
            .filter(Boolean);

        if (!itemKeys.length) {
            return;
        }

        const antiForgery = wrapper.querySelector("input[name='__RequestVerificationToken']");
        if (!(antiForgery instanceof HTMLInputElement) || !antiForgery.value) {
            return;
        }

        try {
            const response = await fetch("/panel/header-bildiri/okundu", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": antiForgery.value
                },
                credentials: "same-origin",
                body: JSON.stringify({ panelKey: panelKey, itemKeys: itemKeys })
            });

            if (!response.ok) {
                return;
            }

            unreadItems.forEach((item) => {
                item.setAttribute("data-header-item-read", "1");
                item.classList.remove("is-unread");
                item.classList.add("is-read");
            });

            const countBadge = wrapper.querySelector(".header-bildiri-count");
            if (countBadge) {
                countBadge.remove();
            }
            wrapper.setAttribute("data-unread-count", "0");
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

        const headUnread = wrapper.querySelector(".header-bildiri-head span");
        if (headUnread) {
            headUnread.textContent = `${count} okunmamis`;
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
                markItemsRead(wrapper);
            }
        });

        wrapper.querySelectorAll("[data-header-item-link]").forEach((link) => {
            link.addEventListener("click", () => {
                markItemsRead(wrapper);
            });
        });
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
