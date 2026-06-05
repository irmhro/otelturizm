(() => {
    const input = document.getElementById('home-q');
    const hint = document.getElementById('home-q-hint');
    const field = input?.closest('.typewriter-field');
    if (!input || !hint || !field) return;

    const samples = [
        "Bodrum'da hafta sonu oteli ara",
        "Kapadokya mağara otelleri",
        "İstanbul Boğaz manzaralı oteller",
        "Antalya all inclusive aile oteli",
        "Çeşme denize sıfır butik otel"
    ];

    let sampleIdx = 0;
    let charIdx = 0;
    let deleting = false;
    let pauseUntil = 0;

    const tick = () => {
        const now = Date.now();
        if (document.activeElement === input || input.value.trim()) {
            field.classList.add('has-value');
            hint.textContent = '';
            setTimeout(tick, 200);
            return;
        }
        field.classList.remove('has-value');

        if (now < pauseUntil) {
            setTimeout(tick, 50);
            return;
        }

        const text = samples[sampleIdx];

        if (!deleting) {
            charIdx += 1;
            hint.textContent = text.slice(0, charIdx);
            if (charIdx >= text.length) {
                deleting = true;
                pauseUntil = now + 1800;
                setTimeout(tick, 80);
                return;
            }
            setTimeout(tick, 55 + Math.random() * 45);
            return;
        }

        charIdx = Math.max(0, charIdx - 2);
        hint.textContent = text.slice(0, charIdx);
        if (charIdx <= 0) {
            deleting = false;
            sampleIdx = (sampleIdx + 1) % samples.length;
            pauseUntil = now + 280;
            setTimeout(tick, 40);
            return;
        }
        setTimeout(tick, 14);
    };

    input.addEventListener('focus', () => field.classList.add('is-active'));
    input.addEventListener('blur', () => field.classList.remove('is-active'));
    input.addEventListener('input', () => {
        field.classList.toggle('has-value', !!input.value.trim());
    });

    tick();
})();
