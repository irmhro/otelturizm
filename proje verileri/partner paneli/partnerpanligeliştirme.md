## Partner Paneli Geliştirme Planı (5 Faz)

Bu doküman, **Partner Paneli**ni uçtan uca bitirmek için 5 fazlı (planlama → geliştirme → test → canlı) yol haritasıdır.
Hedef: tüm sayfalar **tam responsive (mobil öncelikli)**, tüm özellikler **veritabanı ile birebir uyumlu**, performanslı, güvenli ve sürdürülebilir.

### Kapsam (Partner Panel modülleri)
- **Dashboard**
- **Rezervasyonlar** (müşteri bilgileri, durumlar, aksiyonlar)
- **Takvim & Fiyatlar** (günlük fiyat, indirim, kampanya dahil/harici)
- **Oda Yönetimi** (oda ekle/sil/güncelle)
- **Fotoğraflar** (otel/oda görselleri, sıra, silme)
- **Otel Bilgileri** (profil, açıklamalar, konum, politikalar)
- **Değerlendirmeler** (puanlar/yorumlar)
- **Finans** (komisyon, gelir, rapor, export)
- **Performans** (dönüşüm, arama içgörüleri)
- **Tercihler** (ayarlar, bildirimler)
- **Destek / Sohbet**
- **Evrak/Fatura yükleme** (token’lı güvenli upload)

### Temel kurallar (faz geçiş kriteri)
- **Faz tamamlanmadan** bir sonraki faza geçilmez.
- Her faz sonunda:
  - **UI doğrulama** (mobil 390×944 ve 414×896 + desktop)
  - **Yetki kontrolü** (partner kendi oteli dışına erişemez)
  - **Log + hata yönetimi** (kritik aksiyonlar audit’lenir)
  - **Performans**: liste sayfaları server-side pagination ile akıcı

---

## Faz 1 — Panel Shell, Navigasyon, Responsive Altyapı (Temel İskelet)

### 1.1 Hedefler
- Partner panelde tüm sayfalar **tek şablon**: `Header + Sidebar + Content + Footer`.
- Mobilde sidebar **drawer** olur, header sabit kalır, içerik kaydırma tutarlı.
- Ortak UI bileşenleri tanımlanır: badge, status pill, modal, toast, confirm, empty/skeleton.

### 1.2 Yapılacaklar
- **Layout standardı**
  - Sidebar menü öğeleri ve aktif sayfa vurgusu
  - Header: otel adı, kısa özet rozetleri, uyarı/bildirim alanı
  - Footer: sürüm bilgisi, hızlı linkler
- **Responsive standardı**
  - Breakpoint: \(<= 900px\) mobil CSS devreye girer
  - Mobilde tablolar “Excel gibi” görünümde ama yatay taşma kontrollü (sticky column, mini row card fallback)
- **Görsel tasarım standardı**
  - Durum renkleri: Yeşil (aktif), Sarı (beklemede), Kırmızı (hata/iptal), Mor (özel/öneri)
  - Buton stilleri: primary/secondary/danger/ghost

### 1.3 Teslim kriteri
- Panelin tüm sayfaları (HTML şablonları) aynı shell ile açılır.
- Mobilde menü/drawer ve içerik kaydırma **kilitlenmez**, taşma yoktur.

---

## Faz 2 — Veritabanı Uyum Katmanı (Schema + Servisler + Yetkilendirme)

### 2.1 Hedefler
- Panel özellikleri için gereken tüm tablolar/kolonlar netleşir.
- “Eksik tablo/kolon” durumunda sistem kırılmaz (uyumluluk/ensure yaklaşımı).
- Partner scope: her veri sadece ilgili partner’ın oteli/hesabı.

### 2.2 Veri modeli (çekirdek tablolar — taslak)
> Not: İsimler örnek; projedeki mevcut tablo adlarına map edilecek.

- **partner_hesaplari**
  - id, user_id, otel_id, rol, durum, olusturma_tarihi
- **otel_odalar**
  - id, otel_id, oda_adi, kapasite, yatak_tipi, m2, aktif
- **oda_fiyat_takvimi**
  - id, otel_id, oda_id, tarih, normal_fiyat, indirimli_fiyat, para_birimi, durum
- **kampanyalar** / **oda_kampanya_eslesmeleri**
  - kampanya_id, oda_id, baslangic, bitis, aktif
- **rezervasyonlar**
  - id, otel_id, musteri_id, giris, cikis, toplam, komisyon, durum, iptal_nedeni
- **musteriler**
  - id, ad, soyad, telefon, email, kimlik_*
- **yorumlar_degerlendirmeler**
  - id, otel_id, musteri_id, puanlar, yorum, durum
- **sohbet_mesajlari**
  - id, otel_id, musteri_id, partner_id, mesaj, dosya_id, okundu, tarih
- **dosyalar**
  - id, owner_type, owner_id, path, mime, size, sha256, created_at
- **upload_tokenlari**
  - id, token, owner_id, scope, expires_at, single_use, used_at
- **audit_log**
  - id, actor_id, action, entity, entity_id, payload_json, created_at

### 2.3 Servis/Controller katmanı
- `PartnerDashboardService`
- `PartnerReservationService`
- `PartnerRoomService`
- `PartnerPricingService`
- `PartnerMediaService`
- `PartnerFinanceService`
- `PartnerReviewService`
- `PartnerChatService`
- `UploadTokenService` + `SecureFileService`

### 2.4 Teslim kriteri
- Tüm panel sayfalarının ihtiyaç duyduğu API/servis metotları hazır.
- Yetkisiz erişimler kapalı.
- Eksik schema durumunda sistem “sert crash” olmaz.

---

## Faz 3 — Kritik İş Akışları (Rezervasyon + Oda/Fiyat Takvimi + Medya)

### 3.1 Rezervasyonlar
- Liste: sayfabaşı **20**, filtre: tarih aralığı, durum, oda tipi, müşteri adı/telefon
- Detay: popup/drawer, aksiyonlar: onay/iptal/iade notu
- Uyarılar: eksik profil, ödeme bekliyor, iptal süresi vs.

### 3.2 Takvim & Fiyatlar
- Günlük fiyat grid (Excel hissi)
- Toplu güncelleme (tarih aralığına uygula)
- Kampanyaya dahil et/çıkar
- “Normal fiyat / indirimli fiyat” aynı tabloda

### 3.3 Fotoğraflar (otel + oda)
- Upload (çoklu), sıra değiştir, sil, kapak görseli
- Otomatik WebP dönüştürme (kalite koru + boyut düşür)
- Token’lı güvenli upload ve erişim

### 3.4 Teslim kriteri
- Fiyat değişikliği kaydedilir ve listelerde anında görünür.
- Görsel yükleme/silme güvenli ve hızlıdır.
- Rezervasyon yönetimi uçtan uca çalışır.

---

## Faz 4 — Finans + Sohbet + Yorumlar + İçgörüler

### 4.1 Finans
- Dönem seçimi, gelir/komisyon özeti
- Filtrelenebilir hareket listesi
- Export: CSV/XLSX

### 4.2 Sohbet
- Partner–müşteri sohbet listesi
- Okundu/teslim rozetleri
- Dosya/evrak paylaşımı (token + yetki)

### 4.3 Değerlendirmeler
- Puan kırılımı, yorum moderasyonu (yayınla/gizle)
- Şikayet/itiraz akışı (opsiyonel)

### 4.4 İçgörü (MVP)
- “Bölgende şu kadar kişi aradı ama seçmedi”
- “Fiyatın yüksek/çok düşük, şu aralık daha iyi” önerisi

### 4.5 Teslim kriteri
- Finans hesaplamaları doğru, hızlı ve export edilebilir.
- Sohbet güvenli, stabil; yorum ekranı yönetilebilir.

---

## Faz 5 — Kusursuzlaştırma, Test, Canlıya Hazırlık (Release)

### 5.1 Test kapsamı
- E2E senaryolar:
  - fiyat güncelle → liste → detay doğrula
  - rezervasyon filtrele → detay aç → aksiyon uygula
  - görsel upload → webp → sıralama → sil
  - token ile evrak upload → yetkisiz erişim dene
- Mobil: 390×944 ve 414×896
- Desktop: 1280+ ve 1440+

### 5.2 Performans & güvenlik
- Rate limiting (upload/chat)
- Audit log kritik aksiyonlar
- Cache stratejisi: liste ekranları, medya metadata

### 5.3 Canlıya çıkış checklist
- Migration/ensure adımları
- Versiyonlama
- Rollback planı

### 5.4 Teslim kriteri
- Panel “kusursuz”: responsive, güvenli, hızlı, hatasız.
- Canlıya çıkış prosedürü hazır.

---

## Uygulama Sırası (önerilen)
1. Faz 1 (shell) → 2. Faz 2 (schema/services) → 3. Faz 3 (rezervasyon + fiyat + medya) → 4. Faz 4 (finans + sohbet + yorum) → 5. Faz 5 (test + release)

