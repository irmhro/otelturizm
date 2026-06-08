(() => {
    const banner = document.querySelector('[data-dawn-surprise-banner]');
    const openBtn = document.getElementById('dawnSurpriseOpenBtn');
    const modal = document.getElementById('dawnSurpriseModal');
    const box = document.getElementById('dawnSurpriseBox');
    const result = document.getElementById('dawnSurpriseResult');
    const errorEl = document.getElementById('dawnSurpriseError');
    const percentValue = document.getElementById('dawnSurprisePercentValue');
    const countdownEl = document.getElementById('dawnSurpriseCountdown');
    const copyEl = document.querySelector('[data-dawn-surprise-copy]');
    const storageKey = 'otelturizm.dawnSurprise';
    const oneDayMs = 24 * 60 * 60 * 1000;

    if (!banner || !openBtn || !modal || !box || !result || !percentValue || !countdownEl) {
        return;
    }

    let remainingSeconds = 0;
    let countdownTimer = null;

    const formatTime = (seconds) => {
        const safe = Math.max(0, Number(seconds) || 0);
        const hours = Math.floor(safe / 3600);
        const minutes = Math.floor((safe % 3600) / 60);
        const secs = safe % 60;
        if (hours > 0) {
            return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
        }
        return `${String(minutes).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
    };

    const hideError = () => {
        if (errorEl) {
            errorEl.hidden = true;
            errorEl.textContent = '';
        }
    };

    const showError = (message) => {
        if (!errorEl) {
            return;
        }
        errorEl.textContent = message || 'Kutu su an acilamadi. Lutfen tekrar deneyin.';
        errorEl.hidden = false;
        box.hidden = true;
        result.hidden = true;
    };

    const persistClientState = (percent, remaining) => {
        const payload = {
            percent: Number(percent) || 0,
            remainingSeconds: Math.max(0, Number(remaining) || 0),
            expiresAt: Date.now() + (Math.max(0, Number(remaining) || 0) * 1000),
            savedAt: Date.now()
        };
        try {
            localStorage.setItem(storageKey, JSON.stringify(payload));
        } catch {
            // localStorage unavailable
        }
    };

    const readClientState = () => {
        try {
            const raw = localStorage.getItem(storageKey);
            if (!raw) {
                return null;
            }
            const data = JSON.parse(raw);
            const expiresAt = Number(data.expiresAt) || 0;
            if (!expiresAt || expiresAt <= Date.now()) {
                localStorage.removeItem(storageKey);
                return null;
            }
            const remaining = Math.max(0, Math.floor((expiresAt - Date.now()) / 1000));
            return {
                percent: Number(data.percent) || 0,
                remainingSeconds: remaining
            };
        } catch {
            return null;
        }
    };

    const clearClientState = () => {
        try {
            localStorage.removeItem(storageKey);
        } catch {
            // ignore
        }
    };

    const updateCountdown = () => {
        if (remainingSeconds > 0) {
            countdownEl.textContent = `⏱ Kalan süre: ${formatTime(remainingSeconds)} (24 saat geçerli)`;
        } else {
            countdownEl.textContent = '🎁 İndirim 24 saat geçerli';
        }

        if (remainingSeconds <= 0) {
            window.clearInterval(countdownTimer);
            countdownTimer = null;
            banner.classList.remove('is-claimed');
            openBtn.disabled = false;
            openBtn.textContent = 'Kutuyu Aç ve Uygula';
            if (copyEl) {
                copyEl.textContent = 'Hesabınıza özel ek %1 ila %6 arasında rastgele ekstra indirim kazanın.';
            }
            clearClientState();
            return;
        }

        remainingSeconds -= 1;
    };

    const startCountdown = (seconds) => {
        remainingSeconds = Math.max(0, Number(seconds) || 0);
        window.clearInterval(countdownTimer);
        updateCountdown();
        if (remainingSeconds > 0) {
            countdownTimer = window.setInterval(updateCountdown, 1000);
        }
    };

    const applyClaimedUi = (percent) => {
        banner.classList.add('is-claimed');
        openBtn.classList.add('is-applied');
        openBtn.textContent = `%${percent} indirim aktif`;
        openBtn.disabled = true;
        if (copyEl) {
            copyEl.textContent = `Şafak sürpriziniz hazır: rezervasyon toplamından ek %${percent} indirim 24 saat boyunca uygulanacak.`;
        }
    };

    const openModal = () => {
        hideError();
        modal.hidden = false;
        modal.setAttribute('aria-hidden', 'false');
        document.body.style.overflow = 'hidden';
        box.hidden = false;
        box.classList.remove('is-open', 'is-shaking', 'is-burst');
        result.hidden = true;
    };

    const closeModal = () => {
        hideError();
        modal.hidden = true;
        modal.setAttribute('aria-hidden', 'true');
        document.body.style.overflow = '';
    };

    const revealResult = (percent) => {
        percentValue.textContent = String(percent);
        box.classList.add('is-shaking');

        window.setTimeout(() => {
            box.classList.remove('is-shaking');
            box.classList.add('is-open', 'is-burst');
        }, 520);

        window.setTimeout(() => {
            box.hidden = true;
            result.hidden = false;
            result.classList.add('is-visible');
        }, 1200);
    };

    const loadStatus = async () => {
        const cached = readClientState();
        if (cached && cached.percent > 0 && cached.remainingSeconds > 0) {
            applyClaimedUi(cached.percent);
            startCountdown(cached.remainingSeconds);
        }

        try {
            const response = await fetch('/api/dawn-surprise/status', {
                headers: { Accept: 'application/json', 'X-Requested-With': 'XMLHttpRequest' },
                credentials: 'same-origin'
            });
            if (!response.ok) {
                return;
            }

            const data = await response.json();
            if (data.eligible === false) {
                banner.hidden = true;
                return;
            }

            if (data.active && data.percent) {
                applyClaimedUi(data.percent);
                startCountdown(data.remainingSeconds);
                persistClientState(data.percent, data.remainingSeconds);
            }
        } catch {
            // API unavailable — keep local cache if present
        }
    };

    const openBox = async () => {
        openBtn.disabled = true;
        openModal();

        try {
            const response = await fetch('/api/dawn-surprise/open', {
                method: 'POST',
                headers: {
                    Accept: 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                },
                credentials: 'same-origin'
            });
            const data = await response.json().catch(() => ({}));
            if (!response.ok || !data.success || !data.percent) {
                throw new Error(data.message || 'open failed');
            }

            revealResult(data.percent);
            applyClaimedUi(data.percent);
            startCountdown(data.remainingSeconds || Math.floor(oneDayMs / 1000));
            persistClientState(data.percent, data.remainingSeconds || Math.floor(oneDayMs / 1000));
        } catch (err) {
            showError(err && err.message ? String(err.message) : 'Kutu su an acilamadi. Lutfen tekrar deneyin.');
            openBtn.disabled = false;
        }
    };

    openBtn.addEventListener('click', () => {
        if (banner.classList.contains('is-claimed')) {
            return;
        }
        openBox();
    });

    modal.querySelectorAll('[data-dawn-close]').forEach((el) => {
        el.addEventListener('click', closeModal);
    });

    loadStatus();
})();
