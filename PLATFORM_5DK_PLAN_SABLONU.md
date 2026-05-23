# Platform 5 Dakika Plan Şablonu

**Wave ID:** `Wave-___-YYYYMMDD-HHMM` (+5 dk)  
**Koordinatör:** Platform Coordinator · **Onaysız orchestra assign**

## PLAN (1 dk)
| Köşe | Bulgu | P0 |
|------|-------|-----|
| Kamu / Auth / Rezervasyon / Paneller / DB / Build | | |

**P0 bu tur:** T___ · T___ · T___

## EXECUTE (3 dk)
| Stream | Task | Durum |
|--------|------|-------|
| H3 | T350/T356 | |
| H7 | T398 | |

## VERIFY (1 dk)
- [ ] `dotnet build -o .coord-build`
- [ ] `ORKESTRA_DURUM_KONTROL.md` güncellendi
