# Grup 09 — Partner Panel

| Alan | Değer |
|------|-------|
| **Grup ID** | `09` |
| **Upstream** | `03` ✅, `05` ✅, `06` 🔄 |
| **Durum** | 🔄 |

## Ajan listesi

| Ajan | Rol | Grup içi bağımlılık |
|------|-----|---------------------|
| `partner-db` | `OTELLER`, tesis, komisyon SQL | — |
| `partner-ui` | Partner views + CSS + mobil | `partner-db` |

## Dosya kapsamı

```text
Controllers/Paneller/Partner/**
Views/Paneller/Partner/**
wwwroot/assets/css/paneller/partner/**
Models/Paneller/Partner/**
```

## Giriş kriterleri

- [x] **03**, **05** ✅
- [ ] **06** partner shell drift ⏳

## Çıkış kriterleri

- [ ] `PARTNER_PANEL_FULL_PLAN.md` smoke ⏳
- [x] Adres/konum API view binding ✅
- [ ] CTO partner rezervasyon mutlu yol

## Paralelleştirme kuralı

**05 ✅** olmadan partner Razor değişmez. Admin (**08**) ile aynı CSS dosyasına dokunulmaz.

## Atanan görevler (CTO kuyruk)

| Görev ID | Ajan | Durum |
|----------|------|-------|
| T200–T250 | partner-ork | ⏳ Wave 3 |

**Şef:** `partner-ork`

## İletişim

### Tesis profil ✅
### Rezervasyon / bildirim 🔄
### Mobil nav 🔄

**Kaynak:** [Docs/PARTNER_PANEL_FULL_PLAN.md](../../Docs/PARTNER_PANEL_FULL_PLAN.md)
