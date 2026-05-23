# Grup 06 — Frontend CSS/JS

| Alan | Değer |
|------|-------|
| **Grup ID** | `06` |
| **Upstream** | `05` ✅ |
| **Durum** | 🔄 |

## Ajan listesi

| Ajan | Rol | Grup içi bağımlılık |
|------|-----|---------------------|
| `ui-scout` | Sayfa envanteri, `PageCssPath`, mobile.css | — |
| `css-engineer` | Panel/public CSS, BEM/Tabler uyumu | `ui-scout` |
| `fe-cto` | Öncelik sırası, Grup 11 ile hizalama | `css-engineer` |

## Dosya kapsamı

```text
wwwroot/assets/css/**
wwwroot/assets/js/**
!wwwroot/vendor/**
!wwwroot/paneltematabler/**
```

**Yasak:** `Services/**`, `Database/**`.

## Giriş kriterleri

- [x] **05** Razor iskelet ✅
- [x] Route → dosya adı sözleşmesi (AGENTS.md)

## Çıkış kriterleri

- [x] `PageCssPath` → `*.mobile.css` drift (2026-05-22: `meal-services.mobile.css` eklendi)
- [ ] FE-CTO 390px screenshot onayı ⏳
- [ ] `FRONTEND_EKIP_PLAN.md` checklist 🔄
- [x] Public otel liste/detay CSS öncelik sırası tanımlı
- [ ] `dotnet build`

## Paralelleştirme kuralı

**05 ✅** olmadan sayfa CSS’i bağlanmaz. **03** SQL’e dokunulmaz.

## Atanan görevler (CTO kuyruk)

| Görev ID | Ajan | Durum |
|----------|------|-------|
| T070–T090 | ui-scout, css-engineer, fe-cto | ⏳ |
| T072–T074 | fe-otel-public | 🔄 Wave 1 |

**Şef:** `fe-ork` | **Çıkış:** T081 + tüm fe-* SS onay

## İletişim

### `wwwroot/assets/css/paneller/**` 🔄
### `wwwroot/assets/css/oteller/**` 🔄 (öncelik — Grup **11**)
### `wwwroot/assets/js/*.js` ⏳ (Türkçe dosya adı — Grup **13** Faz 4)

**Kaynak:** [FRONTEND_EKIP_PLAN.md](../../FRONTEND_EKIP_PLAN.md)
