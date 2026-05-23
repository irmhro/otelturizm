# Grup 10 — User Panel

| Alan | Değer |
|------|-------|
| **Grup ID** | `10` |
| **Upstream** | `03` ✅, `05` ✅, `06` 🔄 |
| **Durum** | 🔄 |

## Ajan listesi

| Ajan | Rol | Grup içi bağımlılık |
|------|-----|---------------------|
| `user-profile` | Profil, adres ID, favoriler | — |
| `reservations` | Rezervasyon listesi / taslak UI | `user-profile` |

## Dosya kapsamı

```text
Controllers/Paneller/User/**
Views/Paneller/User/**
wwwroot/assets/css/paneller/user/**
Models/Paneller/User/**
Controllers/Reservations/**  (kullanıcı akışı paylaşımlı)
```

## Giriş kriterleri

- [x] **03**, **05** ✅

## Çıkış kriterleri

- [ ] `USER_PANEL_FULL_PLAN.md` ⏳
- [x] Profil adres cascade JS ✅
- [ ] Rezervasyon smoke ⏳

## Paralelleştirme kuralı

**05 ✅** zorunlu. Partner (**09**) CSS glob’una girilmez.

## Atanan görevler (CTO kuyruk)

| Görev ID | Ajan | Durum |
|----------|------|-------|
| T260–T280 | user-ork | ⏳ Wave 3 |

**Şef:** `user-ork`

## İletişim

### Profil / adres ✅
### Favoriler ✅
### Rezervasyonlarım 🔄

**Kaynak:** [Docs/USER_PANEL_FULL_PLAN.md](../../Docs/USER_PANEL_FULL_PLAN.md)
