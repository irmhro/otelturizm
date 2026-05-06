# Partner (otel) paneli — tam gelişim planı

## Ürün sınırları

| Alan | Partner panel | Admin panel |
|------|----------------|-------------|
| Kurumsal **üyelik başvurusu** onayı (tüm platform) | Yok | `Firma Başvuruları` |
| Otel ile **ilişkili** kurumsal firmalar (rezervasyon / firma fiyatı) | `firmalar/talepler` | — |

Partner tarafında “firma başvurusu” ifadesi **yanlış bağlam** oluşturuyordu; ekran artık yalnızca seçili **otel** ile bağlantılı `firmalar` kayıtlarını listeler.

## Menü ↔ route (özet)

- **Rezervasyon:** `rezervasyonlar`, takvim, misafir mesajları, iptal/no-show, ödeme durumları, değerlendirmeler  
- **Fiyat:** takvim fiyatlar, stok, süper fiyat, indirim, kurallar, notlar  
- **Firmalar:** firma oda fiyatları, firma rezervasyonları, analiz, ilişkili kurumsal firmalar  
- **Tesis:** otel bilgisi, özellikler, konum, koşullar, odalar, fotoğraflar, evraklar  
- **Pazarlama:** kampanya, abonelik, performans, konum içgörü, favori misafir, etkinlik  
- **Finans:** finans özeti, ödeme ayarları, faturalar, komisyon, mutabakat  
- **Hesap:** ayarlar, tesis kullanıcıları, güvenlik, bildirim, destek, hesap bilgisi  

## Teknik tamamlanan düzeltme (bu sprint)

- `PartnerService.GetCompanyRequestsAsync`: firma listesi ve başvuru hareketleri **otel bazlı** filtrelendi (`rezervasyonlar` + `firma_oda_fiyat_musaitlik`).  
- Menü ve sayfa başlıkları “talep/başvuru” yerine **ilişkili kurumsal firmalar** olarak güncellendi.

## Kontrol listesi

- [ ] Çok otelli partnerde `otelId` ile sayfa tutarlılığı  
- [ ] Boş liste: otelle ilişkili firma yoksa açıklayıcı boş durum (UI metni)  

## 2026-05-06 Partner Panel Tamamlama Fazları

### Tasarım ve dosya sözleşmesi

- Tüm listeleme ekranları kart grid yerine tablo mantığıyla ilerler: filtre barı, sayfa başı, aksiyon butonları, durum rozeti ve mobil tablo/kart kırılımı zorunludur.
- Her sayfa kendi route adına yakın CSHTML/CSS yapısıyla geliştirilir. Ortak iskelet yalnızca `_PartnerPanelLayout.cshtml`, `_PartnerSidebar.cshtml`, `_PartnerPanelFooter.cshtml`, `shell.css` ve `shell.mobile.css` içinde kalır.
- Sayfa özel CSS dosyaları `wwwroot/assets/css/paneller/partner/{sayfa-adi}.css` ve gerekiyorsa `{sayfa-adi}.mobile.css` olarak tutulur.
- Manuel metinle özellik ekleme kaldırılır; oda/tesis/otel özellikleri tablo sözlüğünden seçilir. Eksik sözlük verisi varsa migration ile tablo ve seed tamamlanır.
- Güvenli dosya/görsel yüklemelerinde otel/oda/evrak bağlamı, tokenlı erişim, WEBP dönüşüm, dosya türü beyaz liste ve fiziksel silme standardı korunur.

### Faz 1 - Kırık ve kritik sayfalar

| Sayfa | Hedef | Durum |
|---|---|---|
| `panel/partner/hesap-bilgileri` | Runtime hatayı kaldır, partner bilgileri formunu kurumsal iki kolonlu form + doğrulanmış özet tabloyla çalıştır. | Tamamlandı ✅ |
| `panel/partner/degerlendirmeler` | Değerlendirme sayfasını tablo + filtre + cevap SLA düzenine geçir. | Tamamlandı ✅ |
| `panel/partner/fiyat/stok-kontenjan` | Oda seçimi, tarih aralığı, satış aç/kapat ve kontenjan güncelleme akışını çalışır hale getir. | Tamamlandı ✅ |
| `panel/partner/firma-fiyatlari` | Firma, oda ve tarih aralığı bazlı çoklu fiyat girişi; birden fazla oda için toplu fiyat belirleme. | Tamamlandı ✅ |

### Faz 2 - Fiyat ve stok alt modülleri

| Sayfa | Hedef | Durum |
|---|---|---|
| `panel/partner/fiyat/super-fiyat` | Seçili oda/tarih aralığında indirimli fiyat + indirim etiketi + satış aç/kapat ile toplu uygulama. | Tamamlandı ✅ |
| `panel/partner/fiyat/indirimler` | Mevcut indirimleri listeler; tarih/oda bazlı indirim + vitrin etiketi + satış aç/kapat toplu uygulama. | Tamamlandı ✅ |
| `panel/partner/fiyat/kisitlamalar` | Min/max geceleme + satış aç/kapat kurallarını tarih/oda bazlı toplu uygulama. | Tamamlandı ✅ |
| `panel/partner/fiyat/gunluk-notlar` | Oda ve gün aralığına operasyon notu (takvim notu) toplu uygulama. | Tamamlandı ✅ |

### Faz 3 - Tesis, oda, koşul ve özellik sözlükleri

| Sayfa | Hedef | Durum |
|---|---|---|
| `panel/partner/tesis/konum` | İl/ilçe/mahalle seçimleri DB sözlüğünden; cascade dropdown + koordinat kaydı. | Tamamlandı ✅ |
| `panel/partner/tesis/kosullar` | Koşul sözlüğü (seçilebilir maddeler) + seçime dayalı kayıt; migration eklendi. | Tamamlandı ✅ |
| `panel/partner/oda-yonetimi` | Oda listesi kart yerine tablo: aktif/pasif, fiyat, stok, kapasite, aksiyonlar. | Tamamlandı ✅ |
| `panel/partner/oda/ozellikler` | Manuel `Yeni özellik ekle` kaldırıldı; oda yönetim formunda DB sözlüğünden özellik seçimi. | Tamamlandı ✅ |

### Faz 4 - Fotoğraf, evrak ve yayın onayı

| Sayfa | Hedef | Durum |
|---|---|---|
| `panel/partner/fotograflar` | Otel görselleri yönetimi + “Oda Fotoğrafları” sekmesi ile oda görsel yönetimine hızlı geçiş. | Tamamlandı ✅ |
| `panel/partner/basvuru-ve-evraklar` | Evrak yüklemede PDF/JPG/PNG/WEBP beyaz liste (UI + server-side doğrulama) + güvenli erişim URL’leri. | Tamamlandı ✅ |
| Admin yayın onayı | Admin panelinde otel `onay_durumu` / `yayin_durumu` filtre ve aksiyonlarıyla yayın kontrolü (onay olmadan yayına alınmaz). | Tamamlandı ✅ |

### Faz 5 - Pazarlama, kampanya, abonelik ve performans

| Sayfa | Hedef | Durum |
|---|---|---|
| `panel/partner/kampanyalar` | Kampanyalar tablo + katıl/ayrıl; katılım listesi ayrı tabloda. | Tamamlandı ✅ |
| `panel/partner/aboneliklerim` | İl/ilçe/mahalle bazlı listeleme başvurusu + admin onaylı sabit sıra süresi (talep tablosu). | Tamamlandı ✅ |
| `panel/partner/performans` | Rezervasyon & gelir trendi + rakip analizi + CSV rapor indir. | Tamamlandı ✅ |
| `panel/partner/pazarlama/konum-icgoruleri` | Misafir şehir/ülke dağılımı + günlük istatistik özeti. | Tamamlandı ✅ |
| `panel/partner/pazarlama/favori-misafirler` | Favori oteller tablosundan anonimleştirilmiş misafir listesi. | Tamamlandı ✅ |
| `panel/partner/pazarlama/etkinlikler` | Kampanya katılım eşleşmeleri + operasyon notu kaydı. | Tamamlandı ✅ |

### Faz 6 - Finans ve hesap

| Sayfa | Hedef | Durum |
|---|---|---|
| `panel/partner/finans` | Gelir/komisyon/vergi/net ödeme özet kartları + son hareketler + faturalar + banka bilgisi formu. | Tamamlandı ✅ |
| `panel/partner/finans/komisyonlar` | Aktif kural + vergi/komisyon özeti + son finans hareketleri tablosu. | Tamamlandı ✅ |
| `panel/partner/finans/mutabakat` | Mutabakat özeti + dönemsel kontrol için son hareketler tablosu + dışa aktarım. | Tamamlandı ✅ |
| `panel/partner/tercihler` | Tercihler UI + `partner_panel_tercihleri` tablosuna kalıcı kayıt (POST/GET). | Tamamlandı ✅ |
| `panel/partner/guvenlik/tesis-kullanicilari` | Davet/iptal akışı + süreli erişim + e‑posta onayı; tablo formatı. | Tamamlandı ✅ |
