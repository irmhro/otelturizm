# Platform 24 Saat Sprint

**Başlangıç:** 2026-05-23  
**Bitiş hedefi:** +24 saat — kullanıcı full build doğrulaması  
**Uzatma (2026-05-24):** Sprint **1 aya** genişletildi → [`PLATFORM_1AY_ORKESTRA_PLAN.md`](PLATFORM_1AY_ORKESTRA_PLAN.md) · sprint: `sprint-1ay-orkestra-20260523`  
**Koordinatör:** Platform Coordinator · Orkestra H1–H14

---

## Mandate

Tüm platform emanet: **en gelişmiş, eksiksiz, şık tasarım** — geçiş animasyonları, kullanıcı bağlılığı, kolaylık, mobil-first.

> **Not:** 24 saatlik sprint kapanış hedefi korunur; gerçek dünya standardı yörüngesi **30 gün / 1440 × 10 dk** ile [`PLATFORM_1AY_ORKESTRA_PLAN.md`](PLATFORM_1AY_ORKESTRA_PLAN.md) üzerinden sürdürülür. Canlı özet: [`geliştirme.md`](geliştirme.md).

| Ritim | Job | Görev |
|-------|-----|--------|
| **10 dk** | `AGENT_LOOP_TICK_platform_coord` | Audit → plan → execute → verify |
| **1 saat** | `AGENT_LOOP_HOURLY_git_sync` | Commit + `git push origin HEAD` |
| **24 saat** | Sprint kapanış (ilk pencere) | Kullanıcı `dotnet build` + smoke |
| **30 gün** | 1 ay orkestra | W1 45% → W4 75% milestone |

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
- 1 ay plan: [`PLATFORM_1AY_ORKESTRA_PLAN.md`](PLATFORM_1AY_ORKESTRA_PLAN.md)
- Sıralı log: [`geliştrme-orkestra.md`](geliştrme-orkestra.md)
- KPI: `ORKESTRA_DURUM_KONTROL.md`
