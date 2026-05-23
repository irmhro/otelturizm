# Grup 04 — Controllers

| Alan | Değer |
|------|-------|
| **Grup ID** | `04` |
| **Upstream** | `02` ✅, `03` ✅ |
| **Durum** | ✅ |

## Ajan listesi

| Ajan | Rol | Grup içi bağımlılık |
|------|-----|---------------------|
| `route-guard` | Attribute route, public URL koruma | — |
| `api-controller` | Api inline SQL + servis delegasyonu | `route-guard` |

## Dosya kapsamı

```text
Controllers/**
!wwwroot/**
```

## Giriş kriterleri

- [x] **02**, **03** ✅
- [x] Inline SQL envanteri (`CONTROLLERS_GELISTIRME.md`)

## Çıkış kriterleri

- [x] Api + Admin inline SQL BÜYÜK HARF ✅
- [ ] Tüm panel controller smoke ⏳
- [x] `dotnet build` (kod derlemesi)
- [ ] Grup **13** için sınıf adı envanteri (Faz 5 backlog) 🔄

## Paralelleştirme kuralı

**03 ✅** olmadan controller’da yeni SQL yazılmaz. Rename (**13**) için sınıf listesi bu grupta dokümante edilir.

## Atanan görevler (CTO kuyruk)

| Görev ID | Ajan | Durum |
|----------|------|-------|
| T040–T045 | ctl-ork | ⏳ |

**Şef:** `ctl-ork`

## İletişim

### `Controllers/Api/` ✅ (Türkçe sınıf, route sabit)
### `Controllers/Paneller/Admin/` 🔄 (SQL ✅, smoke ⏳)
### `Controllers/Paneller/User|Partner|Firma` ⏳ smoke
### Diğer public controllers ⏳

**Kaynak:** [Controllers/CONTROLLERS_GELISTIRME.md](../../Controllers/CONTROLLERS_GELISTIRME.md)

## Blokaj

✅ **2026-05-22 Master CTO:** Api + inline SQL güncel; panel smoke operasyonel (Grup 14). Faz 5 rename backlog Grup **13**'te.

### Master CTO atama
- **Assigned by:** Master CTO
- **Started:** 2026-05-22T14:00Z
- **Completed:** 2026-05-22T15:45Z
