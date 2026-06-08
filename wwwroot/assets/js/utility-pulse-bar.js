(() => {
    const clockRoot = document.querySelector('[data-utility-clock]');
    const timeEl = document.querySelector('[data-utility-time]');
    const rotator = document.querySelector('[data-utility-rotator]');

    const updateClock = () => {
        if (!timeEl) {
            return;
        }

        try {
            const formatter = new Intl.DateTimeFormat('tr-TR', {
                hour: '2-digit',
                minute: '2-digit',
                hour12: false,
                timeZone: 'Europe/Istanbul'
            });
            timeEl.textContent = formatter.format(new Date());
        } catch {
            const now = new Date();
            timeEl.textContent = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;
        }
    };

    updateClock();
    window.setInterval(updateClock, 30000);

    if (!rotator) {
        return;
    }

    const items = Array.from(rotator.querySelectorAll('[data-utility-insight]'));
    if (items.length <= 1) {
        items.forEach((item) => item.classList.add('is-active'));
        return;
    }

    let index = items.findIndex((item) => item.classList.contains('is-active'));
    if (index < 0) {
        index = 0;
        items[0].classList.add('is-active');
    }

    window.setInterval(() => {
        items[index].classList.remove('is-active');
        index = (index + 1) % items.length;
        items[index].classList.add('is-active');
    }, 4200);
})();
