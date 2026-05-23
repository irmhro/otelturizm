# Grup 14 — Canlıya Hazırlık

| Alan | Değer |
|------|-------|
| **Grup ID** | `14` |
| **Upstream** | **01–13** (kademeli) |
| **Durum** | 🔄 (build ✅) |

## Ajan listesi

| Ajan | Rol | Grup içi bağımlılık |
|------|-----|---------------------|
| `build-agent` | `dotnet build`, csproj, log exclude | — |
| `qa-cto` | Smoke, checklist, grup çakışması hakemi | `build-agent` |
| `deploy-doc` | Yayın kılavuzu, migration sırası, yedek | `qa-cto` |

## Dosya kapsamı

```text
otelturizm.csproj
PROJECT_COMPLETION_SUMMARY.md
tools/Release/**
Dokumanlar/Yayin/**
Docs/PLATFORM_MASTER_EXECUTION_ORDER.md
```

**Hakem:** Backend CTO — çapraz grup dosya çakışması.

## Giriş kriterleri

- [ ] Kritik gruplar 01–05 ✅ (mevcut)
- [ ] Panel/frontend grupları minimum 🔄

## Çıkış kriterleri

- [x] `App_Data/logs` build dışı (2026-05-22)
- [x] `dotnet build` 0 hata ✅ (2026-05-22, log exclude sonrası)
- [ ] `GO-LIVE-CHECKLIST` tamam ⏳
- [ ] `PROJECT_COMPLETION_SUMMARY.md` güncel

## Paralelleştirme kuralı

Üretim deploy bu grupta dokümante edilir; **push/deploy kullanıcı onayı** (AGENTS.md). Diğer gruplar bitmeden go-live onayı verilmez.

## Atanan görevler (CTO kuyruk)

| Görev ID | Ajan | Durum |
|----------|------|-------|
| T400–T410 | release-ork, build-agent, qa-cto | ⏳ Wave 4 |

**Şef:** `release-ork` (Master CTO sign-off T410)

## İletişim

### Build / csproj ✅ (log exclude; DLL kilit uyarısı ortam)
### QA smoke ⏳
### Deploy doc ⏳

**Kaynak:** [PROJECT_COMPLETION_SUMMARY.md](../../PROJECT_COMPLETION_SUMMARY.md)

## Geçiş (2026-05-22)

- MSB3030: eksik log kopyası → `DefaultItemExcludes` + `Content Remove` `App_Data/logs`
