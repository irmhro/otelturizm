# Grup 08 — Admin Panel

| Alan | Değer |
|------|-------|
| **Grup ID** | `08` |
| **Upstream** | `03` ✅, `05` ✅, `06` 🔄 |
| **Durum** | 🔄 |

## Ajan listesi

| Ajan | Rol | Grup içi bağımlılık |
|------|-----|---------------------|
| `admin-db` | Admin SQL / RBAC seed uyumu | — |
| `admin-razor` | `Views/Paneller/Admin/**` | `admin-db` |
| `admin-css` | `wwwroot/assets/css/paneller/admin/**` | `admin-razor` |
| `dual-cto` | Admin + shell mobil parity | `admin-css`, `admin-razor` |

## Dosya kapsamı

```text
Controllers/Paneller/Admin/**
Views/Paneller/Admin/**
wwwroot/assets/css/paneller/admin/**
wwwroot/assets/css/panel-admin-shell*
Models/Paneller/Admin/**
```

## Giriş kriterleri

- [x] **03**, **05** ✅
- [x] **06** shell CSS minimum ✅

## Çıkış kriterleri

- [ ] `ADMIN_PANEL_FULL_PLAN.md` tüm fazlar ⏳
- [x] Sidebar/mobil/RBAC düzeltmeleri (2026-05-06 notları)
- [ ] `ADMIN_PANEL_AJAN_GRUBU.md` checklist 🔄
- [ ] `dotnet build` + admin smoke

## Paralelleştirme kuralı

**03+05 ✅** olmadan admin iş kuralı eklenmez. CSS yalnızca admin glob.

## Atanan görevler (CTO kuyruk)

| Görev ID | Ajan | Durum |
|----------|------|-------|
| T140–T190 | admin-ork, fe-admin | ⏳ Wave 2 |

**Şef:** `admin-ork`

## İletişim

### İskelet / menü ✅
### Konaklama yönetimi 🔄
### Placeholder → tablo 🔄

**Kaynaklar:**
- [ADMIN_PANEL_AJAN_GRUBU.md](../../ADMIN_PANEL_AJAN_GRUBU.md)
- [Docs/ADMIN_PANEL_FULL_PLAN.md](../../Docs/ADMIN_PANEL_FULL_PLAN.md)
