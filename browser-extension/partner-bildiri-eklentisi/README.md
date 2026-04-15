# Partner Bildiri Eklentisi

Bu eklenti, partner paneli kapali olsa bile okunmamis bildirimleri kontrol eder.

## Kurulum (Chrome / Edge)

1. Tarayicida `chrome://extensions` (Edge icin `edge://extensions`) acin.
2. `Developer mode` (Gelismis/Gelistirici modu) acik olsun.
3. `Load unpacked` secin.
4. Bu klasoru secin: `browser-extension/partner-bildiri-eklentisi`

## Calisma Mantigi

- Her 1 dakikada bir `https://localhost:7223/panel/header-bildiri/ozet?panelKey=partner` endpointini kontrol eder.
- Okunmamis bildirim varsa:
  - Eklenti badge sayisini gunceller.
  - Tarayici yerel bildirimi gosterir (sistem bildirim sesiyle).
- Popup ekraninda son senkron zamani ve bildirim listesi gorunur.

## Onemli Notlar

- Partner hesabi tarayicida girisli olmalidir (cookie/session aktif).
- Localhost sertifika uyariniz varsa tarayicida bir kez guvenli sekilde acmaniz gerekir.
- Eklenti, sadece `localhost:7223` icin izin ister.
