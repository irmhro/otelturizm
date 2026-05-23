(function () {
    'use strict';

    function markLoaded(img) {
        const wrap = img?.closest?.('.card-image');
        if (wrap) {
            wrap.classList.add('is-loaded');
        }
        img.classList.add('is-loaded');
    }

    function initImageSkeletons() {
        document.querySelectorAll('img.card-main-image').forEach(function (img) {
            if (!(img instanceof HTMLImageElement)) return;
            if (img.complete && img.naturalWidth > 0) {
                markLoaded(img);
                return;
            }
            img.addEventListener('load', function () { markLoaded(img); }, { once: true });
            img.addEventListener('error', function () { markLoaded(img); }, { once: true });
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initImageSkeletons);
    } else {
        initImageSkeletons();
    }
})();

