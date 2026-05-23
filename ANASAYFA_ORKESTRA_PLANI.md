# Anasayfa orkestra planı

Tarih: **2026-05-23**  
Amaç: Anasayfa vitrin alanlarını `/oteller` filtreleri, İstanbul demo otelleri ve partner kampanya katılımı ile uçtan uca bağlamak.

---

## Ajan atamaları

| Grup | Sorumlu | İş | Durum |
|------|---------|-----|-------|
| fe-home | FE-CTO + fe-otel | `_AnasayfaHeader` pill linkleri, `_AnasayfaContent` feature kartları (`filter` + `city=istanbul`) | ✅ |
| svc-hotel | Services | `ResolveListingCampaignTag`, `NormalizeCampaignTag` alias, `CategorySections`, `ApplyHomepageCampaignFilter` | ✅ |
| db-seed | db-ork | `20260523_seed_istanbul_10_ilce_oteller.sql` (10 ilçe demo otel) | ✅ |
| fe-partner | fe-partner | Komisyonlar: günlük/aylık, ödeme günü, tablo, ödendi işaretle | ✅ |
| svc-partner | Services | `GetPartnerCommissionsPageAsync`, `MarkCommissionPaidOnlineAsync` | ✅ |
| qa-smoke | QA | `/oteller/istanbul?filter=budget`, `?etiket=havuzlu-oteller`, partner komisyon ekranı | ⏳ |
| ork-seo | SEO | Kampanya slide `TargetUrl`, hreflang otel listeleme | ⏳ |

---

## Test URL’leri (yerel)

- `/oteller/istanbul?filter=budget`
- `/oteller/istanbul?filter=pool`
- `/oteller/istanbul?etiket=ay-sonu-ozel`
- `/oteller/istanbul?etiket=hafta-sonu-firsatlari`
- `/panel/partner/finans/komisyonlar?otelId={id}`

---

## Bağımlılıklar

1. Yerel DB: mahalle + demo partner + bu seed uygulanmış olmalı.
2. Partner demo: `ork-demo-partner@otelturizm.local` / `Demo123!`
3. Komisyon tablosu dolu değilse liste boş görünür; muhasebe kaydı seed veya rezervasyon akışı ile gelir.
