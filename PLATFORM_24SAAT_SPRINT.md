# Platform 24 Saat Sprint

**Başlangıç:** 2026-05-23  
**Bitiş hedefi:** +24 saat — kullanıcı full build doğrulaması  
**Koordinatör:** Platform Coordinator · Orkestra H1–H14

---

## Mandate

Tüm platform emanet: **en gelişmiş, eksiksiz, şık tasarım** — geçiş animasyonları, kullanıcı bağlılığı, kolaylık, mobil-first.

| Ritim | Job | Görev |
|-------|-----|--------|
| **10 dk** | `AGENT_LOOP_TICK_platform_coord` | Audit → plan → execute → verify |
| **1 saat** | `AGENT_LOOP_HOURLY_git_sync` | Commit + `git push origin HEAD` |
| **24 saat** | Sprint kapanış | Kullanıcı `dotnet build` + smoke |

---

## Saatlik Git protokolü

- Script: `tools/Git/Invoke-HourlyGitSync.ps1`
- Mesaj: `chore(orkestra): saatlik geliştirme snapshot YYYY-MM-DD HH:mm`
- Hariç: `.tmp.driveupload`, `.coord-build*`, `.build-*`, `bin/`, `obj/`
- Log: terminal job çıktısı + `geliştrme-orkestra.md` satır güncellemesi

**Kullanıcı talebi:** Geliştirmeler saatte bir GitHub'a yüklenir (`origin` → `github.com/irmhro/otelturizm`).

---

## Tasarım hedefleri (24h)

1. Kamu: otel liste · harita · detay · kampanya — desktop + mobile CSS (`proje verileri` şablon)
2. Paneller: form sil/düzenle/yükle UX — pro seviye
3. i18n 7 dil · SEO hreflang · e-posta şablonları
4. Admin gelir · komisyon · demo seed · FE-CTO kanıt

---

## İzleme

- Canlı özet: [`geliştirme.md`](geliştirme.md)
- Sıralı log: [`geliştrme-orkestra.md`](geliştrme-orkestra.md)
- KPI: `ORKESTRA_DURUM_KONTROL.md`
