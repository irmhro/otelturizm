# Grup 11 — Public Otel (liste / detay / arama)

| Alan | Değer |
|------|-------|
| **Grup ID** | `11` |
| **Öncelik** | **Frontend öncelik** |
| **Upstream** | `03` ✅, `05` ✅, `06` 🔄 |
| **Durum** | 🔄 |

## Ajan listesi

| Ajan | Rol | Grup içi bağımlılık |
|------|-----|---------------------|
| `liste-agent` | Otel liste, filtre, kart | — |
| `detay-agent` | Detay, presence, fiyat fetch | `liste-agent` |
| `arama-agent` | `OtelAramaApiController` + arama UI | `liste-agent` |

## Dosya kapsamı

```text
Controllers/Oteller/**
Controllers/Anasayfa/**
Controllers/Api/OtelAramaApiController.cs
Views/Oteller/**
Views/Anasayfa/**
wwwroot/assets/css/oteller/**
wwwroot/assets/js/otel-*.js
```

## Giriş kriterleri

- [x] **03** arama/fiyat servisleri ✅
- [x] **05** public views ✅
- [ ] **06** LCP/CSS öncelik backlog 🔄

## Çıkış kriterleri

- [ ] SEO + JSON-LD (`SEO_BOOKING_PARITY_PLAN.md`) ⏳
- [x] Api route `/api/oteller` sabit ✅
- [ ] `fe-cto` (Grup **06**) public CSS sign-off ⏳
- [ ] `dotnet build`

## Paralelleştirme kuralı

**06** ile koordineli: public CSS önce bu grupta. Services SQL **03**’te kalır.

## Atanan görevler (CTO kuyruk)

| Görev ID | Ajan | Durum |
|----------|------|-------|
| T290–T295 | liste-agent, detay-agent, otel-ork | 🔄 Wave 1 |
| T300–T310 | otel-ork | ⏳ |

**Şef:** `otel-ork` | **Çıkış:** T310 + fe-cto SS onay

## İletişim

### Liste / kart 🔄
### Detay / presence ✅
### Arama önerileri ✅

**Kaynak:** [FRONTEND_EKIP_PLAN.md](../../FRONTEND_EKIP_PLAN.md) (Public bölüm), [Docs/SEO_BOOKING_PARITY_PLAN.md](../../Docs/SEO_BOOKING_PARITY_PLAN.md)
