(() => {
    const tickers = document.querySelectorAll('[data-dest-ticker]');
    if (!tickers.length) {
        return;
    }

    tickers.forEach((ticker) => {
        const items = Array.from(ticker.querySelectorAll('[data-dest-ticker-item]'));
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
        }, 3800);
    });
})();
