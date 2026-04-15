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

    if (hasUnread) {
        playAlertSound();
        window.setInterval(() => {
            const stillUnread = Array.from(wrappers).some((wrapper) => Number(wrapper.getAttribute("data-unread-count") || "0") > 0);
            if (stillUnread) {
                playAlertSound();
            }
        }, 5 * 60 * 1000);
    }

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
