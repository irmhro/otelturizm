# Platform Sözleşme — Hukuk Orkestrası (H16)

**Stream:** `H16_ork_hukuk` · **Rol:** Legal-Contract-Ork (“avukat CTO”) · **Owner koordinasyon:** H10_master_cto

> **Yasal uyarı:** Bu doküman ve seed SQL içindeki metinler **hukuki tavsiye değildir**; şablon/placeholder yapıdır. Canlıya almadan önce lisanslı avukat incelemesi zorunludur.

---

## 1. Amaç

Partner, firma ve son kullanıcı için TBK, 6502 sayılı Tüketici Kanunu, Mesafeli Sözleşmeler Yönetmeliği, 6698 KVKK ve platform komisyon modeline uygun **sürümlenebilir sözleşme paketini** tek kaynaktan yönetmek.

---

## 2. Sözleşme türleri

| Tür | Hedef kitle | Slug (v1) | Kabul | Kamu URL |
|-----|-------------|-----------|-------|----------|
| Partner platform | `partner` | `partner-platform-sozlesmesi` | Panel onboarding | `/sozlesmeler/partner-platform-sozlesmesi` |
| Partner KVKK aydınlatma | `partner` | `partner-kvkk-aydinlatma` | E-posta/panel | `/sozlesmeler/partner-kvkk-aydinlatma` |
| Komisyon + mesafeli satış ek | `partner` | `komisyon-mesafeli-satis-ek` | Ek protokol | `/sozlesmeler/komisyon-mesafeli-satis-ek` |
| Firma kurumsal | `company` | `firma-kurumsal-platform-sozlesmesi` | Firma onboarding | `/sozlesmeler/firma-kurumsal-platform-sozlesmesi` |
| Kullanıcı koşulları | `user` | `kullanici-kullanim-kosullari` | Site | mevcut seed |
| Kullanıcı KVKK | `user` | `kullanici-kvkk-aydinlatma` | Site/footer | mevcut seed |

---

## 3. Türk hukuku referansları (yüksek seviye)

| Alan | Mevzuat / ilke | Platform uygulaması |
|------|----------------|---------------------|
| Genel borçlar | TBK (sözleşme serbestisi, ayıp, ifa) | Partner platform sözleşmesi taraflar/konu |
| Tüketici | 6502 s.k. | Ön bilgilendirme, iptal/iade otel politikasında |
| Mesafeli satış | Mesafeli Sözleşmeler Yönetmeliği | Ek protokol + rezervasyon özeti |
| Kişisel veri | 6698 KVKK | Aydınlatma metinleri, md. 11 başvuru kanalı |
| Elektronik delil | 6098 HMK (ör. kayıt) | SOZLESME_KABULLERI, gönderim logları |
| Yetkili mahkeme | TBK 23 / sözleşme serbestisi | İstanbul (Çağlayan) — seed §12 |

---

## 4. Placeholder sözlüğü (admin düzenlenebilir)

| Token | Açıklama |
|-------|----------|
| `[PARTNER_UNVAN]` | Partner ticari unvan |
| `[PARTNER_VERGI]` | Vergi dairesi / no |
| `[PARTNER_ADRES]` | Tebligat adresi |
| `[PARTNER_YETKILI]` | Yetkili ad-soyad |
| `[PLATFORM_UNVAN]` | Otelturizm işletmeci unvanı |
| `[KOMISYON_ORANI]` | Paneldeki güncel oran |
| `[BASLANGIC_TARIHI]` / `[YURURLUK_TARIHI]` | Yürürlük |
| `[FESIH_BILDIRIM_GUN]` | Fesih ihbar süresi (ör. 30) |
| `[MUTABAKAT_EPOSTA]` | muhasebe@… |
| `[FIRMA_UNVAN]` | Kurumsal firma |

---

## 5. Seed ve migration

**Dosya:** `Database/MigrationsSql/veri/migrationlar/20260524_seed_platform_sozlesmeler.sql`

- Idempotent: `IF NOT EXISTS … SLUG + VERSIYON_NO`
- UTF-8 BOM önerilir (sqlcmd)
- Canlı öncesi: full backup + script sırası dokümantasyonu

---

## 6. Uygulama bağlantıları

| Katman | Bileşen |
|--------|---------|
| Kamu | `ContractsController` → `/sozlesmeler/{slug}` |
| Admin | `AdminPanelController.Contracts` → `/admin/sozlesmeler` |
| Servis | `IContractContentService.GetPublicContractBySlugAsync` |
| E-posta | `EmailTemplateService` sözleşme bildirimi |
| Kabul | `SOZLESME_KABULLERI`, `SOZLESME_GONDERIM_LOGLARI` |

**Wave-A1:** Partner platform iskeleti seed'lendi; admin Contracts üzerinden HTML/PDF güncellenir.

---

## 7. H16 görev kuyruğu

| ID | Görev | Durum |
|----|-------|-------|
| H16-01 | Partner platform v1 şablon (seed) | ✅ Wave-A1 |
| H16-02 | Firma kurumsal v1 şablon | ✅ seed |
| H16-03 | Avukat review checklist PDF | 🔴 |
| H16-04 | Versiyonlama UI (admin yeni versiyon) | 🟡 P1 |
| H16-05 | Otomatik kabul hatırlatma (e-posta) | 🟡 P1 |

---

## 8. Orkestra bağlantısı

- `CTO_AJAN_ATAMA_KUYRUGU.md` → `H16_ork_hukuk`
- `PLATFORM_1AY_ORKESTRA_PLAN.md` → Hafta 2 admin + hukuk seed
- `geliştrme-orkestra.md` → #067+
