# Komisyon & Rezervasyon Taktik Planı (H2+H3)

**Kaynak:** `Docs/PLATFORM_DUNYA_DEVLERI_YOL_HARITASI.md` §3 · **Wave:** III · **Owner:** H2 (partner gelir), H3 (admin gelir)

Bu belge 15 gelir/komisyon taktik maddesini, uygulama notlarını ve Wave-III durumunu özetler.

---

## Özet durum (Wave-III bu tur)

| Durum | Taktik |
|-------|--------|
| ✅ Uygulandı | T1 Son 30 gün komisyon KPI (partner), T5 Platform GMV 30 gün (admin) |
| 📋 Stub / backlog | T2–T4, T6–T15 (aşağıda notlar) |

---

## 15 taktik

### T1 — Son 30 gün komisyon özeti (partner KPI şeridi) · P0 · T383

**Hedef:** Partner `finans/komisyonlar` sayfasında rolling 30 gün komisyon görünürlüğü (Booking/Expedia partner earnings benzeri).

**Uygulama notları:**
- `PartnerService.PopulateCommissionsPageDataAsync` → `KOMISYON_MUHASEBE_KAYITLARI` üzerinde `KAYIT_TARIHI >= DATEADD(day,-30,...)`.
- ViewModel: `PartnerFinancePageViewModel.Last30DaySummaryCards` (3 kart: Toplam / Ödenen / Bekleyen).
- View: `Views/Paneller/Partner/Commissions.cshtml` + `finance.css` (`.partner-commission-30d-grid`).
- **Durum:** ✅ Wave-III uygulandı.

---

### T2 — Dönemsel komisyon trend grafiği · P0 · T383

**Hedef:** Son 6–12 ay aylık komisyon çubuk/çizgi grafiği; MoM % değişim rozeti.

**Uygulama notları:**
- SQL: `GROUP BY YEAR(KAYIT_TARIHI), MONTH` + `SUM(KOMISYON_TUTARI)` filtre `OTEL_ID`.
- ViewModel: `List<PartnerChartBarViewModel>` (admin dashboard `ReservationChart` ile aynı desen).
- UI: Commissions sayfasında KPI şeridinin altında; Chart.js veya saf CSS bar (admin dashboard ile tutarlı).
- Cache: 15 dk partner-scoped memory cache (opsiyonel).
- **Durum:** 📋 Backlog.

---

### T3 — Tahmini payout (ödeme) tarihi · P0

**Hedef:** `OTELLER.ODEME_VADESI` + son ödenmemiş tahakkuk → “Tahmini transfer: 12 Haziran” metni.

**Uygulama notları:**
- Mevcut `CommissionPaymentDayText` genişlet: iş günü hesabı (`BusinessCalendarHelper` stub).
- Bekleyen tutar > 0 ise ETA; değilse “Güncel — bekleyen yok”.
- **Durum:** 📋 Backlog (vade metni sayfada var; ETA hesabı yok).

---

### T4 — Komisyon CSV + PDF dönem özeti · P0

**Hedef:** Seçili filtre aralığı için muhasebe özeti export (partner self-serve).

**Uygulama notları:**
- CSV: mevcut `finans/disa-aktar` action’ına `scope=commission` parametresi veya yeni `ExportCommissionsCsv`.
- PDF: QuestPDF / mevcut fatura PDF altyapısı reuse; şablon: otel, dönem, brüt/net komisyon tablosu.
- **Durum:** 📋 Backlog.

---

### T5 — Admin Platform GMV (30 gün) widget satırı · P0 · T350

**Hedef:** Admin dashboard ikinci KPI satırı: platform GMV, 30 gün komisyon, rezervasyon adedi.

**Uygulama notları:**
- `AdminService.GetDashboardAsync` → `REZERVASYONLAR` aggregate (`DURUM <> İptal`, `OLUSTURULMA_TARIHI` son 30 gün).
- ViewModel: `AdminDashboardViewModel.Revenue30DayMetrics`.
- View: `Dashboard.cshtml` “Platform GMV (30 gün)” bölümü → `/admin/raporlar`.
- **Durum:** ✅ Wave-III uygulandı.

---

### T6 — Platform GMV vs partner net kırılım · P0 · T350 genişletme

**Hedef:** Admin tek ekranda platform brüt GMV ile partner net ödeme farkını görmek (margin insight).

**Uygulama notları:**
- SQL: platform `SUM(TOPLAM_TUTAR)` vs `SUM(OTELE_ODENECEK_TUTAR)` veya `KOMISYON_MUHASEBE` net.
- UI: Reports veya Commerce Insight sayfasında çift eksen mini chart; dashboard’a 4. kart olarak eklenebilir.
- **Durum:** 📋 Backlog (GMV 30 gün kartı var; net kırılım kartı yok).

---

### T7 — Yüksek iptal anomali uyarısı (admin) · P0

**Hedef:** Otel bazlı iptal oranı > eşik (ör. %20) → dashboard uyarı rozeti.

**Uygulama notları:**
- Mevcut `HotelKpis.CancelRatePercent` kullan; `>= 20` → `admin-action-tile danger` veya ayrı “Riskli tesisler” listesi.
- Opsiyonel: `ADMIN_ISLEM_LOGLARI` kaydı.
- **Durum:** 📋 Backlog (tabloda renk var; proaktif uyarı kuyruğu yok).

---

### T8 — Rezervasyon doluluk heatmap (admin) · P1 · T384

**Hedef:** Admin rezervasyon modülünde oda/gün doluluk ısı haritası.

**Uygulama notları:**
- SQL: `REZERVASYONLAR` × `GIRIS/CIKIS` → günlük dolu oda sayısı matrisi.
- View: `UnifiedReservations` veya yeni `OccupancyHeatmap.cshtml`; CSS grid 7×N.
- **Durum:** 📋 Stub.

---

### T9 — No-show risk skoru (stub) · P1 · T384

**Hedef:** Rezervasyon satırında 0–100 risk skoru; partner/admin listelerde rozet.

**Uygulama notları:**
- **Stub servis:** `NoShowRiskScoringService.ScoreAsync(reservationId)` → sabit kurallar: ön ödeme yok +1, geç check-in saati +1, iptal geçmişi +2 → 0–100 normalize.
- DB kolonu yok; runtime hesap; ileride ML tablosu `rezervasyon_risk_skorlari`.
- UI: `partner-status-pill--warning` “Orta risk” / tooltip kurallar.
- **Durum:** 📋 Stub (servis + UI yok).

---

### T10 — Kampanya katılım → rezervasyon ROI · P1 · T385

**Hedef:** Partner kampanya sayfasında katılımın getirdiği rezervasyon ve komisyon.

**Uygulama notları:**
- Attribution: `REZERVASYONLAR.KAMPANYA_ID` veya UTM `kaynak` alanı; `COUNT` + `SUM(KOMISYON)`.
- `PartnerCampaigns` SummaryCards’a “ROI” kartı; dönem filtresi kampanya başlangıcı ile hizalı.
- **Durum:** 📋 Backlog.

---

### T11 — Firma B2B toplu rezervasyon komisyon kırılımı · P1 · T386

**Hedef:** Kurumsal firma panelinde limit uyarısı + komisyon satır kırılımı.

**Uygulama notları:**
- `FIRMALAR` limit alanları + `REZERVASYONLAR.FIRMA_ID` aggregate.
- Uyarı: limit %80 → sarı banner.
- **Durum:** 📋 Backlog.

---

### T12 — Satış paneli pipeline + komisyon projeksiyon · P2 · T387

**Hedef:** Lead → teklif → rezervasyon hunisi; ağırlıklı komisyon tahmini.

**Uygulama notları:**
- CRM tabloları yoksa stub `SalesPipelineService` in-memory demo.
- Aşama olasılığı × ortalama komisyon oranı × ortalama sepet.
- **Durum:** 📋 Stub.

---

### T13 — Dinamik komisyon kural motoru (stub) · P2 · T388

**Hedef:** Sezon/ilçe bazlı kural önerisi; admin onayı ile `KOMISYON_VERGILER` güncelleme.

**Uygulama notları:**
- `CommissionRuleEngine.SuggestAsync(hotelId)` → JSON kural önerisi; persist yok.
- Admin Commissions sayfasında “Önerilen kural” accordion (read-only).
- **Durum:** 📋 Stub.

---

### T14 — Komisyon MoM karşılaştırma rozeti · P1

**Hedef:** Partner 30 gün KPI’da önceki 30 güne göre % değişim (↑↓).

**Uygulama notları:**
- İkinci SQL penceresi: gün 31–60; `(current-previous)/previous` → `TrendText` on stat card.
- T1 kartlarına `Description` içine ekle.
- **Durum:** 📋 Backlog.

---

### T15 — Partner payout ETA geri sayım · P1

**Hedef:** Bekleyen komisyon > 0 ise header’da “X gün kaldı” chip.

**Uygulama notları:**
- T3 ETA + `Shell` alert band; `PartnerPanelController.Commissions` ViewBag.
- **Durum:** 📋 Backlog.

---

## Teknik referanslar (uygulanan)

| Bileşen | Dosya |
|---------|--------|
| Partner 30g SQL + kartlar | `Services/PartnerService.cs` (`PopulateCommissionsPageDataAsync`) |
| Partner view | `Views/Paneller/Partner/Commissions.cshtml` |
| Admin 30g GMV | `Services/AdminService.cs` (`GetDashboardAsync`) |
| Admin view | `Views/Paneller/Admin/Dashboard.cshtml` |
| CSS | `wwwroot/assets/css/paneller/partner/finance.css` |

---

## Sonraki dalga önerisi (H2)

1. T2 trend grafiği (partner commissions).
2. T3 + T15 payout ETA.
3. T9 no-show stub servis + rezervasyon listesinde rozet.

---

*Oluşturulma: Wave-III revenue tactics agent · Build: `.build-wave3-revenue`*
