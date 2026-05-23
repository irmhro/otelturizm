# Platform paket satışı — 5651 / 5661 loglama (Partner → Otel)

**Koordinatör:** `PLATFORM_KOORDINATOR_OPERASYON_PLANI.md`  
**Orkestra:** Wave H (T315–T330) · `CTO_AJAN_ATAMA_KUYRUGU.md`

---

## 1. İş modeli

| Aktör | Rol |
|-------|-----|
| **Platform (Admin)** | Paket kataloğu, fiyat, görseller, özellikler, başvuru onayı/red, aktivasyon |
| **Partner** | Yetkili otellerine paket sunar; otelin 5661 kurulu olup olmadığını beyan eder; başvuru açar |
| **Otel** | Dolaylı — partner üzerinden hizmet alır; uyumluluk bayrağı `OTEL_UYUM_DURUMLARI` |

**Hedef kural örneği:** `OTEL_5661_YOK` — paket yalnızca otelde 5661 loglama sistemi **kurulu değilse** listelenir (partner satış fırsatı).

---

## 2. Veri modeli

| Tablo | Amaç |
|-------|------|
| `PLATFORM_PAKET_KATEGORILERI` | 5651, 5661, Paket, Danışmanlık |
| `PLATFORM_PAKETLER` | Katalog: kod, başlık, fiyat, periyot, görseller JSON, özellikler JSON, hedef kural |
| `OTEL_UYUM_DURUMLARI` | Otel başına 5651/5661 kurulum bayrağı |
| `PARTNER_PAKET_BASVURULARI` | Başvuru → onay → aktif yaşam döngüsü |

**Durumlar (başvuru):** `Beklemede` → `Onaylandi` / `Reddedildi` → `Aktif` → `Iptal` / `SuresiDoldu`

---

## 3. Panel rotaları

| Panel | GET | POST |
|-------|-----|------|
| Partner | `/panel/partner/platform-paketleri` | `.../basvuru-olustur` |
| Partner | `/panel/partner/platform-paketleri/detay/{id}` | — |
| Admin | `/admin/platform-paketleri` | `.../basvuru-guncelle`, `.../paket-kaydet` |

**Yetki:** `admin.platform_packages` (seed migration)

---

## 4. UX (mobil öncelik)

- **Katalog:** Kart grid, kapak görseli, fiyat/periyot, “5661 gerekli” rozeti  
- **Detay:** Galeri, özellik listesi, sözleşme linki, başvuru formu (otel seçili)  
- **Başvurular:** Tablo → mobil kart; durum rozetleri  
- **Admin:** Özet KPI + paket tablosu + başvuru kuyruğu (onay/red/not)

---

## 5. Orkestra görevleri (Wave H)

| ID | Orkestra | İş | Durum |
|----|----------|-----|-------|
| T315 | db-ork | Tablo migration + index | 🔄 kurulum |
| T316 | db-ork | Seed kategori + 3 demo paket | 🔄 |
| T317 | grup-03 | `PlatformPackageService` | 🔄 |
| T318 | grup-04 | Partner + Admin controller | 🔄 |
| T319 | grup-05 | CSHTML + mobile.css | 🔄 |
| T320 | grup-07 | CSRF + audit log başvuru | pending |
| T321 | fe-partner | Katalog/detay SS + FE-CTO | pending |
| T322 | fe-admin | Admin paket SS | pending |
| T323 | ork-medya | Paket görsel upload WebP | pending |
| T324 | models-services | Ödeme/fatura entegrasyonu (Faz 2) | pending |
| T325 | master-cto | Onay merkezi ile birleştirme | pending |

---

## 6. Faz 2 (sonraki sprint)

- Online ödeme / fatura kesimi  
- Partner komisyon payı paket satışından  
- Otomatik provisioning (5651 log sunucusu API)  
- E-posta şablonu: başvuru alındı / onaylandı  

---

## 7. Doğrulama

1. Migration idempotent uygulanır  
2. Partner katalogda 3 paket görür  
3. `OTEL_5661_YOK` kuralı ile 5661 kurulu otelde paket gizlenir  
4. Başvuru admin kuyruğunda görünür, onay → `Aktif`  
5. `dotnet build` 0 hata  
