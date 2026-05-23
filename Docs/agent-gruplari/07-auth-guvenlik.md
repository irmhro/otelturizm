# Grup 07 — Auth & Güvenlik

| Alan | Değer |
|------|-------|
| **Grup ID** | `07` |
| **Upstream** | `02` ✅, `03` ✅ |
| **Durum** | 🔄 |

## Ajan listesi

| Ajan | Rol | Grup içi bağımlılık |
|------|-----|---------------------|
| `auth-sql` | `KULLANICILAR`, oturum, şifre hash SQL | — |
| `2fa-agent` | Telefon/WhatsApp doğrulama akışı | `auth-sql` |
| `csrf-auditor` | CSRF, rate limit, CSP rapor | `auth-sql` |

## Dosya kapsamı

```text
Controllers/Login/**
Controllers/Register/**
Controllers/Security/**
Controllers/TelefonDogrulama/**
Services/**/Auth*
Services/**/Security*
Services/**/Phone*
Views/Giris/**
Views/Register/**
Views/TelefonDogrulama/**
```

## Giriş kriterleri

- [x] **02**, **03** ✅
- [x] `SECURITY_PLATFORM_PLAN.md` faz 1 tanımlı

## Çıkış kriterleri

- [ ] `/health/*` tam doğrulama ⏳
- [ ] CSRF + rate limit regression ⏳
- [x] Auth SQL BÜYÜK HARF (spot)
- [ ] CTO checklist `SECURITY_PLATFORM_PLAN.md`

## Paralelleştirme kuralı

**03 ✅** olmadan auth servis SQL’i değişmez. Middleware değişikliği **14** ile koordine.

## Atanan görevler (CTO kuyruk)

| Görev ID | Ajan | Durum |
|----------|------|-------|
| T130–T135 | sec-ork | 🔄 Wave 1 |

**Şef:** `sec-ork`

## İletişim

### Login / Register views ✅
### 2FA / webhook 🔄
### CSP / CspReport ⏳

**Kaynak:** [Docs/SECURITY_PLATFORM_PLAN.md](../../Docs/SECURITY_PLATFORM_PLAN.md)
