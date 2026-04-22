# Otelturizm Panel Design Sozlesmesi

Bu dosya, admin/partner/firma panellerinde kullanilan tasarim ve fonksiyon gelistirme sozlesmesidir.
Temel tasarim sistemi kaynak dokuman: `d:/otelturizm-transfer-20260421/peneldesing.md`.

## Tasarim Kurallari (Zorunlu)

- Renk/spacing/radius degiskenleri merkezi `:root` yapisinda tutulur.
- Buton, kart, tablo, rozet ve form siniflari ortak bir tasarim diliyle ilerler.
- Mobil oncelikli responsive yaklasim uygulanir.
- Tum admin sayfalari ayni layout/shell icerisinde tutarli render edilir.

## Admin Panel Kapsam Sozlesmesi

Asagidaki moduller veritabani baglantili calismalidir:

1. Partner basvurulari
2. Firma basvurulari
3. Kullanicilar
4. Platform yetkilileri
5. Acik oteller
6. Bekleyen oteller
7. Degerlendirmeler
8. Komisyonlar
9. Otel bazli gelir / komisyon ozetleri
10. Dosya yukleme / silme / kapali-acik durum aksiyonlari

## Gelistirme Gunlugu

### 2026-04-22

- Admin paneline yeni veritabani liste sayfalari eklendi:
  - `firma-basvurulari`
  - `platform-yetkilileri`
  - `acik-oteller`
  - `bekleyen-oteller`
- Sol menude bu moduller icin yeni gezinme butonlari eklendi.
- `AdminService` tarafinda bu moduller icin:
  - sayfa konfigurasyonlari
  - ozet kart SQL tanimlari
  - tablo SQL sorgulari
  eklendi.
- Komisyon ekrani genisletildi:
  - otel bazli `toplam gelir / toplam komisyon / odenen komisyon / bekleyen komisyon` tablosu eklendi.
- Dashboard KPI kartlari genisletildi:
  - bekleyen partner
  - bekleyen firma
  - acik otel
  - bekleyen otel

## Sonraki Faz (Kodlanacak)

- Kullanici / platform yetkili durum degistirme aksiyon butonlari (aktif-pasif, rol atama).
- Firma basvuru karar akisinda "onayla / askiya al / reddet" islemleri.
- Bekleyen oteller icin hizli onay/yayin aksiyonlari.
- Degerlendirme moderasyonunda toplu onay/red/isaretleme.
- Komisyon muhasebe kayitlari icin durum degistirme (beklemede/odendi/itiraz).
- Dosya yukleme/silme operasyonlarinin tum admin modullerine standartlastirilmasi.
