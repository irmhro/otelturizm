# Kalan orkestra planı — dürüst durum + atama

Tarih: **2026-05-22**  
Master CTO: `MASTER_CTO_OFIS.md` | Görev kuyruğu: `CTO_AJAN_ATAMA_KUYRUGU.md`

---

## Kısa cevap (sorularınız)

| Soru | Cevap |
|------|--------|
| Tüm proje dosyaları ajanlarca güncellendi mi? | **Hayır** — DB/Services/Views/Models büyük ölçüde ✅; **FE-CTO onay ~5/130+ sayfa** |
| Paneller orkestra ile tam geliştirildi mi? | **Kısmen** — mobil CSS çoğu sayfada var; **iç sayfa SS + onay + satış/kullanıcı/departman eksik** |
| Otel liste/detay/admin/firma/partner tam kapasite mi? | **Hayır** — liste+harita ✅; detay ⏳; panellerde çoğunlukla giriş kapısı SS |
| Dosya adları alan adıyla eşleşti mi? | **Kısmen** — `Controllers/Api` ✅ Türkçe; **Panel controller’lar İngilizce** (`AdminPanelController` vb.) |
| Siber güvenlik + SEO tam mı? | **Plan var, uygulama devam** — aşağıdaki Wave D–F |

---

## Tamamlanan (kanıtlı)

- SQL şema uyumu (Services uppercase, stale `FROM users` = 0)
- Views 345/345 şema metinleri (`VIEWS_GELISTIRME.md`)
- Models adres ID alanları (`MODELS_GELISTIRME.md`)
- Koordinat: ILLER/ILCELER/MAHALLELER dolu (yerel DB)
- Api controller Türkçe adlar (`TURKCE_DOSYA_ADLANDIRMA_PLAN.md`)
- Build: 0 hata (izole output ile)

---

## Eksik envanter (sayfa / alan)

| Orkestra | CSHTML | mobile.css | FE-CTO | Not |
|----------|--------|------------|--------|-----|
| fe-otel-public | 3 | ✅ | 2/3 | OtelDetay SS + seed |
| fe-admin | 55 | çoğu ✅ | ~1 | İç sayfalar auth SS |
| fe-partner | 47 | çoğu ✅ | ~1 | Aynı |
| fe-firma | 12 | kısmi | ~1 | Aynı |
| **fe-user** | **16** | çoğu ✅ | **0** | Plan dışı kalmıştı |
| **fe-satis** | **13** | kısmi | **0** | Plan dışı |
| **fe-departman** | ~5 | kısmi | **0** | Plan dışı |
| Auth/login/register | ~8 | ✅ | 0 | SS döngüsü |
| Email şablonları | ~100+ | — | — | İşlevsel; FE ayrı |
| **ork-seo** | sitemap/hreflang | — | 0 | Çok dilli arama |
| **ork-guvenlik** | middleware/RBAC | — | kısmi | Tam denetim |

**Toplam FE-CTO hedefi:** ~**150+** sayfa (117 plan + user/satis/departman)

---

## Yeni orkestratörler (Wave D–F)

| ID | Orkestratör | Şef CTO | Ajanlar | Kapsam |
|----|-------------|---------|---------|--------|
| **D1** | fe-user | Frontend CTO | ui-scout, css, razor | 16 kullanıcı paneli |
| **D2** | fe-satis | Frontend CTO | ui-scout, css, razor | 13 satış paneli |
| **D3** | fe-departman | Frontend CTO | ui-scout, css | departman paneli |
| **E1** | ork-seo-global | SEO CTO | hreflang, sitemap, json-ld, meta | `Seo/`, `Locale/`, otel URL, çok dil |
| **E2** | ork-guvenlik | Security CTO | csrf, rate-limit, csp, audit | `SECURITY_PLATFORM_PLAN.md` |
| **E3** | ork-veri | Data CTO | KVKK, şifreleme, log maskeleme | PII, connection, secure files |
| **F1** | ork-turkce-faz2 | Backend CTO | rename + ref-patch | Panel controller Türkçe (route korunur) |

---

## SEO hedefi (yabancı kullanıcı kendi dilinde arayınca)

1. `hreflang` + canonical (`Views`, layout, `SitemapController`)
2. Otel/şehir sayfalarında çok dilli `title`/`description` (DB veya resource)
3. Structured data `Hotel`, `BreadcrumbList` (otel detay/liste)
4. `robots.txt` + dinamik sitemap dil segmentleri
5. URL slug: mevcut `/oteller/{slug}` korunur; dil prefix `/en/`, `/de/` … (`LocaleController`)

**Kabul kriteri:** SEO CTO checklist `docs/SEO_BOOKING_PARITY_PLAN.md` maddeleri ✅

---

## Güvenlik hedefi (CSRF açılmadan)

- Tüm POST: `[ValidateAntiForgeryToken]` veya API anti-forgery header
- Rate limit: login, register, webhook, public API
- CSP + `CspReportController` raporlama
- Admin RBAC: migration seed uygulandı mı kontrol
- Secure file download: `SecureFilesController` yetki

**Kabul kriteri:** Security CTO `docs/SECURITY_PLATFORM_PLAN.md` Faz 1 ✅

---

## Türkçe dosya adı — Faz 2 hedefi

| Eski | Önerilen | Route korunur |
|------|----------|---------------|
| `AdminPanelController` | `YonetimPanelController` | `/admin/*` |
| `PartnerPanelController` | `PartnerPanelController` veya `IsOrtagiPanelController` | `/panel/partner/*` |
| `FirmaPanelController` | `FirmaPanelController` | `/panel/firma/*` |
| `UserPanelController` | `KullaniciPanelController` | `/panel/kullanici/*` |
| `SalesPanelController` | `SatisPanelController` | `/panel/satis/*` |
| `OtellerController` | `OtellerController` | ✅ zaten Türkçe klasör |
| `AuthController` | `KimlikController` | `/giris`, `/kayit` route attribute |

---

## Master CTO onay sırası (her görev)

`in_progress` → kod → **Backend CTO** → **Frontend CTO** (390px SS) → **Security CTO** (POST formlar) → **SEO CTO** (public sayfa) → **Master CTO** `done`

---

## İlk 15 görev (kuyruk genişletme T100+)

| ID | Orkestratör | Görev |
|----|-------------|-------|
| T100 | fe-otel | Local DB örnek otel seed → OtelDetay 20 SS |
| T101 | fe-admin | Test admin → Dashboard + SystemHealth SS |
| T102 | fe-partner | Test partner → FacilityLocation SS |
| T103 | fe-user | Profile + Reservations mobil SS |
| T104 | fe-satis | Dashboard + CreateReservation SS |
| T105 | ork-seo | hreflang layout + otel detay JSON-LD |
| T106 | ork-guvenlik | Auth POST CSRF denetim raporu |
| T107 | ork-veri | Log/PII maskeleme grep |
| T108 | fe-admin | HelpCenter kaydet smoke |
| T109 | Models+Services | MISAFIR_*_ID persist (rezervasyon) |
| T110 | Master CTO | Build + PROJECT_COMPLETION güncelle |

---

*Bu plan arka plan orkestralarına devredildi; commit/deploy yok.*
