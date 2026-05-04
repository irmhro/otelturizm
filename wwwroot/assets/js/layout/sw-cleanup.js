// SW temizliği: sadece problem tespiti veya manuel tetik ile.
// - Otomatik: sadece sayfa bir önceki yüklemede SW kaynaklı asset hatası işaretlendiyse
// - Manuel: URL'e ?swfix=1 eklenirse
(function () {
    try {
        if (!('serviceWorker' in navigator)) return;
        const doneKey = 'otelturizm_sw_cleanup_done_v3';
        const errKey = 'otelturizm_sw_asset_error_v1';
        const host = (window.location && window.location.hostname || '').toLowerCase();
        const isLocalhost = host === 'localhost' || host === '127.0.0.1' || host === '::1' || host.endsWith('.localhost');

        const params = new URLSearchParams(window.location.search || '');
        const manual = params.get('swfix') === '1';
        const flagged = localStorage.getItem(errKey) === '1';
        if (sessionStorage.getItem(doneKey) === '1') return;
        navigator.serviceWorker.getRegistrations().then(function (regs) {
            const hasLocalRegs = !!((regs || []).filter(function (r) {
                const swUrl = (r && ((r.active && r.active.scriptURL) || (r.waiting && r.waiting.scriptURL) || (r.installing && r.installing.scriptURL) || '')) || '';
                return isLocalhost && swUrl.indexOf(host) !== -1;
            }).length);
            if (!manual && !flagged && !hasLocalRegs) return;

            sessionStorage.setItem(doneKey, '1');
            localStorage.removeItem(errKey);
            if (regs && regs.length) {
                regs.forEach(function (r) { try { r.unregister(); } catch (e) { } });
            }
            if (window.caches && caches.keys) {
                caches.keys().then(function (keys) {
                    (keys || []).forEach(function (k) { try { caches.delete(k); } catch (e) { } });
                });
            }
            setTimeout(function () {
                try {
                    const clean = window.location.pathname + window.location.hash;
                    window.location.replace(clean);
                } catch (e) { }
            }, 200);
        });
    } catch (e) { }
})();

