# Grup 05 — Views (Razor)

| Alan | Değer |
|------|-------|
| **Grup ID** | `05` |
| **Upstream** | `02` ✅, `04` 🔄 (Api/route ✅, panel smoke devam) |
| **Durum** | ✅ |

### Master CTO atama
- **Assigned by:** Master CTO
- **Started:** 2026-05-22T14:20Z
- **Completed:** 2026-05-22T15:20Z

## Ajan listesi

| Ajan | Rol | Grup içi bağımlılık |
|------|-----|---------------------|
| `razor-binder` | ViewModel binding, adres JS, fetch URL | — |

## Dosya kapsamı

```text
Views/**
Pages/**
```

**Yasak:** `Services/**` SQL; ağır CSS (**06**).

## Giriş kriterleri

- [x] Adres API route’ları sabit (`/api/adres/*`)
- [x] ViewModel alanları **02** ile uyumlu

## Çıkış kriterleri

- [x] `VIEWS_GELISTIRME.md` ana akışlar ✅
- [x] Hardcoded eski controller adı yok
- [x] `dotnet build`

## Paralelleştirme kuralı

**04** route değişikliği yapmadan fetch URL güncellenmez. **06** CSS’i bu grup taşımaz (yalnızca `PageCssPath` referansı).

## Atanan görevler (CTO kuyruk)

| Görev ID | Ajan | Durum |
|----------|------|-------|
| T050–T060 | view-ork | ⏳ Wave 2 |

**Şef:** `view-ork`

## İletişim

### Auth / Register ✅
### Paneller ✅
### Oteller public ✅
### Email şablonları ✅ (statik model)

**Kaynak:** [Views/VIEWS_GELISTIRME.md](../../Views/VIEWS_GELISTIRME.md)
