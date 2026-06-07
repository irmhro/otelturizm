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

    if (!banner || !openBtn || !modal || !box || !result || !percentValue || !countdownEl) {
        return;
    }

    let remainingSeconds = 0;
    let countdownTimer = null;

    const formatTime = (seconds) => {
        const safe = Math.max(0, Number(seconds) || 0);
        const mm = String(Math.floor(safe / 60)).padStart(2, '0');
        const ss = String(safe % 60).padStart(2, '0');
        return `${mm}:${ss}`;
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
        try {
            sessionStorage.setItem(storageKey, JSON.stringify({
                percent: Number(percent) || 0,
                remainingSeconds: Math.max(0, Number(remaining) || 0),
                savedAt: Date.now()
            }));
        } catch {
            // sessionStorage unavailable
        }
    };

    const updateCountdown = () => {
        countdownEl.textContent = `⏱ Kalan Süre: ${formatTime(remainingSeconds)}`;
        if (remainingSeconds <= 0) {
            window.clearInterval(countdownTimer);
            countdownTimer = null;
            banner.classList.remove('is-claimed');
            openBtn.disabled = false;
            openBtn.textContent = 'Kutuyu Aç ve Uygula';
            if (copyEl) {
                copyEl.textContent = 'Hesabınıza özel ek %3 ila %10 arasında rastgele ekstra indirim kazanın.';
            }
            try {
                sessionStorage.removeItem(storageKey);
            } catch {
                // ignore
            }
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
            copyEl.textContent = `Şafak sürpriziniz hazır: rezervasyon toplamından ek %${percent} indirim uygulanacak.`;
        }
    };

    const openModal = () => {
        hideError();
        modal.hidden = false;
        modal.setAttribute('aria-hidden', 'false');
        document.body.style.overflow = 'hidden';
        box.hidden = false;
        box.classList.remove('is-open');
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
        window.setTimeout(() => {
            box.classList.add('is-open');
        }, 120);
        window.setTimeout(() => {
            box.hidden = true;
            result.hidden = false;
        }, 900);
    };

    const loadStatus = async () => {
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
            } else {
                startCountdown(15 * 60);
            }
        } catch {
            startCountdown(15 * 60);
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
            startCountdown(data.remainingSeconds);
            persistClientState(data.percent, data.remainingSeconds);
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
