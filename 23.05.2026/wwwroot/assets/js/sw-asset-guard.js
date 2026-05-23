// SW kaynakli asset problemlerini tespit edip tek seferlik "swfix" tetiklemek icin bayraklar.
// Bu dosya hafif tutulur; sadece vendor/assets fetch hatalarini izler.
(function () {
  try {
    var errKey = 'otelturizm_sw_asset_error_v1';
    if (localStorage.getItem(errKey) === '1') return;

    // Basit sinyal: asset script/css yükleme hatası
    window.addEventListener('error', function (e) {
      try {
        var target = e && e.target;
        var src = (target && (target.src || target.href)) || '';
        if (!src) return;
        if (src.indexOf('/assets/') === -1 && src.indexOf('/lib/') === -1 && src.indexOf('/vendor/') === -1) return;
        localStorage.setItem(errKey, '1');
      } catch (_) { }
    }, true);
  } catch (_) { }
})();

