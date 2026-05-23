# Grup 13 — Türkçe Dosya Adları

| Alan | Değer |
|------|-------|
| **Grup ID** | `13` |
| **Upstream** | `04` 🔄, `05` ✅ |
| **Durum** | 🔄 |

## Ajan listesi

| Ajan | Rol | Grup içi bağımlılık |
|------|-----|---------------------|
| `rename-agent` | Dosya/sınıf rename, route koruma | — |
| `ref-patcher` | `using`, DI, view referansları | `rename-agent` |

## Dosya kapsamı

```text
Controllers/**  (önce Controllers/Api — tamamlandı)
Models/**       (Api DTO — tamamlandı)
wwwroot/assets/js/**  (Faz 4 — endpoint sabit)
```

**Sıra:** `Controllers/Api` ✅ → diğer `Controllers/**` ⏳ → CSS/JS ⏳

## Giriş kriterleri

- [x] **04** Api sınıf envanteri ✅
- [x] Public HTTP URL değişmeyecek (plan onayı)

## Çıkış kriterleri

- [x] Faz 1 Api ✅
- [x] Faz 2 modeller ✅
- [ ] Faz 5 panel controller rename ⏳
- [ ] `dotnet build` her faz sonrası
- [ ] `rg` eski sınıf adı kalmadı (hedef faz)

## Paralelleştirme kuralı

**04** sınıf listesi dokümante edilmeden panel rename yok. **06** endpoint path’lerini değiştirmez.

## Atanan görevler (CTO kuyruk)

| Görev ID | Ajan | Durum |
|----------|------|-------|
| T330 | rename-ork | ✅ done |
| T331–T335 | rename-ork | ⏳ |

**Şef:** `rename-ork`

## İletişim

### Faz 1 Api ✅
### Faz 2 Models ✅
### Faz 3 Views ✅ (gerek yok)
### Faz 4 JS ⏳
### Faz 5 Controllers ⏳

**Kaynak:** [TURKCE_DOSYA_ADLANDIRMA_PLAN.md](../../TURKCE_DOSYA_ADLANDIRMA_PLAN.md)

## Blokaj

🔄 Faz 5 bekliyor — **04** `route-guard` envanter onayı sonrası toplu rename.
