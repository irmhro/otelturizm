// SW temizliği: sadece problem tespiti veya manuel tetik ile.
// - Otomatik: sadece sayfa bir önceki yüklemede SW kaynaklı asset hatası işaretlendiyse
// - Manuel: URL'e ?swfix=1 eklenirse
(function () {
    try {
        if (!('serviceWorker' in navigator)) return;
        const doneKey = 'otelturizm_sw_cleanup_done_v2';
        const errKey = 'otelturizm_sw_asset_error_v1';

        const params = new URLSearchParams(window.location.search || '');
        const manual = params.get('swfix') === '1';
        const flagged = localStorage.getItem(errKey) === '1';
        if (!manual && !flagged) return;

        if (sessionStorage.getItem(doneKey) === '1') return;
        sessionStorage.setItem(doneKey, '1');
        localStorage.removeItem(errKey);

        navigator.serviceWorker.getRegistrations().then(function (regs) {
            if (regs && regs.length) {
                regs.forEach(function (r) { try { r.unregister(); } catch (e) { } });
            }
            if (window.caches && caches.keys) {
                caches.keys().then(function (keys) {
                    (keys || []).forEach(function (k) { try { caches.delete(k); } catch (e) { } });
                });
            }
            // Sadece ihtiyaç olduğunda 1 kez yenile.
            setTimeout(function () {
                try { window.location.replace(window.location.pathname + window.location.hash); } catch (e) { }
            }, 200);
        });
    } catch (e) { }
})();

