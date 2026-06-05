(() => {
    const root = document.querySelector('[data-hero-campaign-slider]');
    if (!root) return;

    const track = root.querySelector('[data-hero-campaign-track]');
    const slides = track ? Array.from(track.querySelectorAll('.hero-campaign-slide')) : [];
    const dots = root.querySelectorAll('.hero-campaign-dot');
    if (!track || slides.length <= 1) return;

    const prefersReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const intervalMs = prefersReducedMotion ? 0 : 1500;
    let index = 0;
    let timerId = 0;

    const setActive = (nextIndex) => {
        index = (nextIndex + slides.length) % slides.length;
        track.style.transform = `translate3d(-${index * 100}%, 0, 0)`;
        dots.forEach((dot, dotIndex) => {
            dot.classList.toggle('is-active', dotIndex === index);
        });
    };

    const tick = () => {
        setActive(index + 1);
    };

    if (intervalMs > 0) {
        timerId = window.setInterval(tick, intervalMs);
        document.addEventListener('visibilitychange', () => {
            if (document.hidden) {
                window.clearInterval(timerId);
                return;
            }

            window.clearInterval(timerId);
            timerId = window.setInterval(tick, intervalMs);
        });
    }
})();
