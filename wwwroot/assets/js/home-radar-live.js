(() => {
    const cards = document.querySelectorAll('[data-radar-card]');
    if (!cards.length) return;

    const formatTimer = (totalSec) => {
        const sec = Math.max(0, totalSec);
        const m = Math.floor(sec / 60);
        const s = sec % 60;
        return `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
    };

    const renderLive = (card) => {
        const liveEl = card.querySelector('[data-radar-live]');
        if (!liveEl) return;

        const type = card.getAttribute('data-radar-live-type') || 'viewers';
        const value = parseInt(card.getAttribute('data-radar-live-value') || '0', 10);

        if (type === 'occupancy') {
            liveEl.innerHTML = `<i class="fas fa-bolt" aria-hidden="true"></i> %${value} Doluluk`;
        } else {
            liveEl.innerHTML = `<i class="fas fa-fire" aria-hidden="true"></i> ${value} Kişi İnceliyor`;
        }
    };

    const renderTimer = (card) => {
        const timerEl = card.querySelector('[data-radar-timer]');
        if (!timerEl) return;
        const sec = parseInt(card.getAttribute('data-radar-timer-sec') || '0', 10);
        timerEl.innerHTML = `<i class="far fa-clock" aria-hidden="true"></i> ${formatTimer(sec)}`;
    };

    cards.forEach(card => {
        renderLive(card);
        renderTimer(card);
    });

    window.setInterval(() => {
        cards.forEach(card => {
            const type = card.getAttribute('data-radar-live-type') || 'viewers';
            let value = parseInt(card.getAttribute('data-radar-live-value') || '0', 10);
            let timerSec = parseInt(card.getAttribute('data-radar-timer-sec') || '0', 10);

            if (Math.random() > 0.35) {
                if (type === 'occupancy') {
                    value += Math.random() > 0.5 ? 1 : -1;
                    value = Math.min(99, Math.max(82, value));
                } else {
                    value += Math.random() > 0.5 ? 1 : -1;
                    value = Math.min(28, Math.max(3, value));
                }
                card.setAttribute('data-radar-live-value', String(value));
                renderLive(card);
            }

            if (timerSec > 0) {
                timerSec -= 1;
                card.setAttribute('data-radar-timer-sec', String(timerSec));
                renderTimer(card);
            }
        });
    }, 5000);
})();
