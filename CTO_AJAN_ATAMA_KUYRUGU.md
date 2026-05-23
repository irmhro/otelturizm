# CTO Ajan Atama Kuyruğu

```yaml
sprint_id: sprint-continuous-infinite-20260523
updated: 2026-05-26T10:40:00+03:00
cycle_interval: 10m
delegation_policy: kullanici_onaysiz_10dk_wave_assign
active_wave: Wave-X-20260526-integrasyon
agent_loop_job: AGENT_LOOP_TICK_platform_coord
chief_engineer: platform-coordinator
assignment_doc: ORKESTRA_TAM_GOREVLENDIRME.md
coordinator_plan: PLATFORM_KOORDINATOR_OPERASYON_PLANI.md
continuous_cycle: PLATFORM_SUREKLI_GELISTIRME_DONGUSU.md
plan_template: PLATFORM_10DK_PLAN_SABLONU.md
gap_analysis: Docs/PLATFORM_OZELLIK_GAP_ANALIZI.md
admin_roadmap: Docs/ADMIN_PANEL_MASTER_ROADMAP.md
active_wave: Wave-X-20260526-integrasyon
queue_active_parallel: [H13_i18n_ui, H9_ork_seo, H14_email_ork, H4_fe_user, H11_finans_komisyon]
next_wave: Wave-VI-20260526-1050
build_status: pass
build_note: "2026-05-26 10:40: dotnet build -o .coord-build — 0 hata, 0 uyarı (Wave-V cycle #1)"
fe_cto_approved: "6/151"
canliya_hazir: hayir
delegation_policy: kullanici_onaysiz_10dk_wave_assign
corner_audit_wave_v:
  hotels_39_seed: pass
  meal_filter_kahvalti_dahil: pass
  auth_all_panels: fail
  reservation_address: partial
  campaign_discounted_price: partial
  admin_partner_commission_widgets: partial

parallel_streams:
  H1_fe_otel_public: { lead: Frontend-Ork-Kamu, status: done, tasks: [T005,T006,T007,T306,T307,T304], note: "2026-05-23 build .build-h1 pass; pill etiket URLs; paneller/otel CSS; Lighthouse hints" }
  H2_fe_partner: { lead: Partner-FE-Ork, status: assigned, tasks: [T311], note: "T102,T200,T201,T202,T309,T321 done; T311 batch-1 → Docs/ORKESTRA_PANEL_SS_BATCH.md (10 sayfa route+view)" }
  H3_admin_master: { lead: Admin-FE-Ork, status: in_progress, tasks: [T101,T108,T111,T112,T113,T114,T115,T210,T310,T322,T327,T329,T330,T353,T354,T355,T356,T357], note: "Infinite loop P0 admin master roadmap; bulk + fraud views (T350 Revenue Center done)" }
  H3_fe_admin: { lead: Admin-FE-Ork, alias: H3_admin_master, status: in_progress, tasks: [T101,T108,T111,T112,T113,T114,T115,T210,T310,T322] }
  H4_fe_user: { lead: User-FE-Ork, status: done, tasks: [T103,T120,T121,T122,T230], note: "profile+reservations safe-area; Invoices PageCssMobile; build .build-h4 pass" }
  H5_fe_satis: { lead: Satis-FE-Ork, status: done, tasks: [T104,T130,T131,T132], evidence: "shell.mobile safe-area; dashboard PageCssMobile; build pass" }
  H6_fe_firma: { lead: Firma-FE-Ork, status: done, tasks: [T220,T140,T141], evidence: "shell.mobile safe-area; CreateReservation CompanyTotal query E2E; build pass" }
  H7_ork_guvenlik: { lead: Security-Ork, status: done, tasks: [T004,T301,T302,T149,T320] }
  H8_ork_backend: { lead: DB-Services-Ork, status: done, tasks: [T308,T313,T107,T303,T341,T342,T343,T344,T345], note: "Istanbul 39 ilce demo seed T341-T345 tamam; localdb verify OK" }
  H9_ork_seo: { lead: SEO-Ork, status: done, tasks: [T148,T305] }
  H10_master_cto: { lead: Master-CTO, status: assigned, tasks: [T150,T250,T314,T325], run: after_H1-H9 }

waves:
  wave_a:
    status: assigned
    teams:
      - { id: grup-07-security, status: done, tasks: [{ id: T004, status: done, owner: H7 }] }
      - { id: fe-otel-public, status: done, tasks: [{ id: T005, status: done, note: "SS path docs/frontend-screenshots/fe-otel-public" }, { id: T006, status: done, note: "liste mobil FAB+map CTA touch" }, { id: T007, status: done, note: "harita mobil back/cta 44px" }] }
  wave_b:
    status: assigned
    teams:
      - { id: grup-03-services, status: assigned, owner: H8 }
      - { id: grup-02-models, status: assigned, owner: H8 }
      - { id: grup-05-views, status: assigned, owner: H1-H6 }
  wave_c:
    status: assigned
    teams:
      - { id: fe-admin, pages: 55, status: assigned, owner: H3 }
      - { id: fe-partner, pages: 47, status: assigned, owner: H2 }
      - { id: fe-firma, pages: 16, status: in_progress, owner: H6, note: "T220+T140-T141 done; kalan sayfa SS kısmi" }
      - { id: fe-user, pages: 17, status: done, owner: H4 }
      - { id: fe-satis, pages: 13, status: done, owner: H5, note: "T104+T130-T132 shell+dashboard mobile" }
      - { id: fe-departman, pages: 5, status: assigned, owner: H6 }
  wave_d:
    status: assigned
    teams:
      - { id: D1-fe-user, status: done, tasks: [{ id: T103, status: done, note: "profile.mobile+reservations.mobile safe-area" }] }
      - { id: D2-fe-satis, status: done, tasks: [{ id: T104, status: done }] }
      - { id: D3-fe-departman, status: assigned, tasks: [{ id: T120, status: assigned }] }
  wave_e:
    status: assigned
    teams:
      - { id: E3-ork-veri, status: done, tasks: [{ id: T107, status: done, owner: H8 }] }
  wave_f:
    status: assigned
    teams:
      - { id: fe-otel-public, tasks: [{ id: T101, status: assigned, owner: H3 }, { id: T102, status: done, owner: H2 }] }
  wave_g:
    status: assigned
    teams:
      - { id: G1-ork-guvenlik, tasks: [{ id: T301, status: done }, { id: T302, status: done }] }
      - { id: G2-ork-medya-perf, tasks: [{ id: T303, status: done, owner: H8 }, { id: T304, status: done, owner: H1 }] }
      - { id: G3-ork-seo-kamu, tasks: [{ id: T305, status: done, owner: H9 }, { id: T307, status: done, owner: H1 }] }
      - { id: G4-fe-otel-public, tasks: [{ id: T306, status: done }, { id: T308, status: done, owner: H8 }] }
      - { id: G5-fe-partner, tasks: [{ id: T309, status: done }] }
      - { id: G6-fe-panels-ss, tasks: [{ id: T310, status: assigned }, { id: T311, status: assigned }, { id: T312, status: in_progress, note: "fe-user paths partial" }] }
      - { id: G7-models-e2e, tasks: [{ id: T313, status: assigned }] }
      - { id: G8-master-cto, tasks: [{ id: T314, status: assigned, owner: H10 }] }
  wave_h:
    status: assigned
    teams:
      - { id: H-fe-partner-paket, tasks: [{ id: T321, status: done }] }
      - { id: H-fe-admin-paket, tasks: [{ id: T322, status: assigned }] }
      - { id: H-ork-guvenlik-paket, tasks: [{ id: T320, status: done }] }
      - { id: H-faz2-odeme, tasks: [{ id: T324, status: assigned }, { id: T325, status: assigned }] }
  wave_i:
    status: closed
    wave_id: Wave-I-20260523-0100
    cadence: "30dk PLAN → EXECUTE → VERIFY → PLAN"
    teams:
      - { id: I-build-gate, owner: H8, tasks: [{ id: T326, status: done }, { id: T349, status: done, note: "coord-build 0 error after restore" }] }
      - { id: I-admin-master-p0, owner: H3, tasks: [{ id: T350, status: done, note: "/admin/gelir-merkezi RevenueCommandCenter" }, { id: T353, status: assigned }, { id: T354, status: assigned }, { id: T355, status: assigned }, { id: T356, status: done, note: "Hotels bulk publish POST /admin/oteller/toplu-yayin + audit" }, { id: T357, status: assigned }] }
      - { id: I-admin-workflow, owner: H3, tasks: [{ id: T329, status: assigned }, { id: T330, status: assigned }] }
      - { id: I-fe-partner-ss, owner: H2, tasks: [{ id: T311, status: assigned }] }
      - { id: I-fe-otel-public, owner: H1, tasks: [{ id: T327, status: assigned }, { id: T328, status: assigned }] }
      - { id: I-backend-integrasyon, owner: H8, tasks: [{ id: T360, status: assigned }, { id: T361, status: assigned }, { id: T362, status: assigned }, { id: T341, status: done }, { id: T342, status: done }, { id: T343, status: done }, { id: T344, status: done }, { id: T345, status: done }] }
      - { id: I-master-gates, owner: H10, tasks: [{ id: T336, status: assigned }, { id: T337, status: assigned }, { id: T338, status: assigned }] }
      - { id: I-roadmap-p1-p3, owner: H3, tasks: [{ id: T358, status: assigned }, { id: T359, status: assigned }, { id: T363, status: assigned }, { id: T364, status: assigned }, { id: T365, status: assigned }, { id: T366, status: assigned }, { id: T367, status: assigned }, { id: T368, status: assigned }, { id: T369, status: assigned }, { id: T370, status: assigned }] }
  wave_ii:
    status: closed
    wave_id: Wave-II-20260523-0130
    cadence: "30dk PLAN → EXECUTE → VERIFY → PLAN"
    delegation_policy: kullanici_onaysiz_30dk_wave_assign
    teams:
      - { id: II-build-gate, owner: H8, tasks: [{ id: T349, status: done, note: "OtelListeleme Razor — coord-build 0 error (pre-existing pass)" }] }
      - { id: II-demo-ork-ist, owner: H8, tasks: [{ id: T341, status: done }, { id: T342, status: done, note: "Install-IstanbulIlceDemo.ps1; medya seed OK; tam otel seed dosya adi script alias disi (20260526_seed_istanbul_ilce_oteller_tam.sql)" }] }
      - { id: II-admin-slowsql, owner: H3, tasks: [{ id: T353, status: done, note: "/admin/slow-sql SlowSqlMonitor + SlowSql.cshtml + sidebar" }] }
      - { id: II-admin-p0-next, owner: H3, tasks: [{ id: T350, status: done, note: "Revenue Command Center gelir-merkezi" }, { id: T356, status: done, note: "Bulk hotel publish Hotels.cshtml" }, { id: T357, status: assigned }] }
  wave_iii:
    status: closed
    wave_id: Wave-III-20260526-0200
    cadence: "30dk PLAN → EXECUTE → VERIFY → PLAN"
    mandate: Docs/PLATFORM_DUNYA_DEVLERI_YOL_HARITASI.md
    teams:
      - { id: III-user-attraction-h1, owner: H1, tasks: [{ id: T372, status: assigned }, { id: T375, status: assigned }, { id: T376, status: assigned }, { id: T381, status: assigned }] }
      - { id: III-user-attraction-h4, owner: H4, tasks: [{ id: T371, status: assigned }, { id: T373, status: assigned }] }
      - { id: III-partner-revenue, owner: H2, tasks: [{ id: T383, status: assigned }, { id: T311, status: assigned }] }
      - { id: III-admin-master-p0, owner: H3, tasks: [{ id: T350, status: done, note: "RevenueCommandCenter + revenue-command-center.css" }, { id: T353, status: done, note: "Wave-II SlowSql view" }, { id: T354, status: assigned }, { id: T355, status: assigned }, { id: T356, status: done, note: "Bulk publish/unpublish + panel-admin-hotels.mobile" }, { id: T384, status: assigned }, { id: T385, status: assigned }] }
      - { id: III-security-fraud, owner: H7, tasks: [{ id: T357, status: assigned }] }
      - { id: III-seo-schema, owner: H9, tasks: [{ id: T389, status: assigned }, { id: T390, status: assigned }] }
      - { id: III-backend-wave-iv-stub, owner: H8, tasks: [{ id: T374, status: assigned }, { id: T377, status: assigned }, { id: T378, status: assigned }, { id: T379, status: assigned }, { id: T380, status: assigned }, { id: T382, status: assigned }, { id: T386, status: assigned }, { id: T387, status: assigned }, { id: T388, status: assigned }] }
      - { id: III-master-gates, owner: H10, tasks: [{ id: T349, status: done, note: "Wave-III build gate .coord-build" }] }
  wave_iv:
    status: closed
    wave_id: Wave-IV-20260526-1030
    cadence: "30dk corner audit — Docs/PLATFORM_TAM_KONTROL_AUDIT_WAVE-IV.md"
    note: "T391-T401 backlog; meal filter PASS; build .coord-build pass"
  wave_v:
    status: active
    wave_id: Wave-V-20260526-1040
    cadence: "10dk PLAN → EXECUTE → VERIFY → PLAN"
    mandate: "PLATFORM_10DK_PLAN_SABLONU.md + köşe audit ORKESTRA"
    queue_p0:
      - { id: T350, owner: H3, priority: P0, title: "Admin Revenue Command Center", status: done, note: "/admin/gelir-merkezi + admin.reports RBAC" }
      - { id: T356, owner: H3, priority: P0, title: "Bulk hotel publish admin", status: done, note: "POST BulkUpdateHotelPublishStatus + Hotels batch UI; build .build-t356" }
      - { id: T398, owner: H7, priority: P0, title: "Sales panel login redirect + auth E2E", status: assigned }
    teams:
      - { id: V-admin-p0, owner: H3, tasks: [{ id: T350, status: done, note: "Wave-V Revenue Command Center" }, { id: T356, status: done, note: "Wave-V bulk hotel publish" }] }
      - { id: V-security-auth, owner: H7, tasks: [{ id: T398, status: assigned }, { id: T399, status: queued }] }
      - { id: V-build-gate, owner: H10, tasks: [{ id: T349, status: done, note: "Wave-V .coord-build 0 error" }] }

tasks_registry:
  T004: { orch: H7, status: done, priority: P0 }
  T005: { orch: H1, status: done, priority: P0, note: "SS path documented fe-otel-public" }
  T006: { orch: H1, status: done, priority: P0, note: "otel-listeleme.mobile map CTA touch" }
  T007: { orch: H1, status: done, priority: P0, note: "haritaoteller.mobile touch targets" }
  T101: { orch: H3, status: in_progress, priority: P0, note: "Dashboard PageCssMobile wired; auth SS blocked on test user" }
  T102: { orch: H2, status: done, priority: P0 }
  T103: { orch: H4, status: done, priority: P1, note: "profile.mobile+reservations.mobile env(safe-area-inset-*)" }
  T104: { orch: H5, status: done, priority: P2, evidence: "satis shell.mobile.css safe-area + viewport-fit" }
  T107: { orch: H8, status: done, priority: P1, deliverable: docs/PII_LOGGING_H8_NOTE.md }
  T108: { orch: H3, status: assigned, priority: P1 }
  T111: { orch: H3, status: in_progress, batch: admin-ss-1, page: Reservations }
  T112: { orch: H3, status: in_progress, batch: admin-ss-1, page: Hotels }
  T113: { orch: H3, status: in_progress, batch: admin-ss-1, page: ApprovalCenter }
  T114: { orch: H3, status: in_progress, batch: admin-ss-2, page: Security }
  T115: { orch: H3, status: in_progress, batch: admin-ss-2, page: PlatformPackages }
  T120: { orch: H4, status: done, note: "SS path fe-user/dashboard FRONTEND_ORKESTRATOR_PLAN" }
  T121: { orch: H4, status: done, note: "SS path fe-user/favorilerim" }
  T122: { orch: H4, status: done, note: "SS path fe-user/guvenlik" }
  T130: { orch: H5, status: done, evidence: "satis dashboard.mobile grid" }
  T131: { orch: H5, status: done, evidence: "satis layout PageCssMobile hook" }
  T132: { orch: H5, status: done, evidence: "FRONTEND_ORKESTRATOR_PLAN H5 notu" }
  T140: { orch: H6, status: done, evidence: "firma shell.mobile safe-area" }
  T141: { orch: H6, status: done, evidence: "firma dashboard PageCssMobile" }
  T148: { orch: H9, status: done, priority: P1 }
  T149: { orch: H7, status: done, priority: P0 }
  T150: { orch: H10, status: assigned, priority: final }
  T200: { orch: H2, status: done }
  T201: { orch: H2, status: done }
  T202: { orch: H2, status: done, priority: P0 }
  T210: { orch: H3, status: assigned }
  T220: { orch: H6, status: done, evidence: "CreateReservation POST redirect preserves dates; CompanyTotal rebuild" }
  T230: { orch: H4, status: done, note: "Invoices.cshtml + invoices.mobile.css PageCssMobile; /panel/user/faturalarim" }
  T250: { orch: H10, status: assigned }
  T301: { orch: H7, status: done, priority: P0 }
  T302: { orch: H7, status: done, priority: P0 }
  T303: { orch: H8, status: done, priority: P1 }
  T304: { orch: H1, status: done, priority: P1, note: "preload/lazy/fetchpriority kamu 3lu" }
  T305: { orch: H9, status: assigned, priority: P0 }
  T306: { orch: H1, status: done, priority: P0, note: "paneller/otel/otel-detay CSS alias; FRONTEND_ORKESTRATOR_PLAN" }
  T307: { orch: H1, status: done, priority: P0, note: "anasayfa pill/feature etiket= canonical" }
  T308: { orch: H8, status: done, priority: P1, deliverable: Database/MigrationsSql/veri/migrationlar/20260523_seed_10_istanbul_demo_oteller.sql }
  T309: { orch: H2, status: done, priority: P0 }
  T310: { orch: H3, status: in_progress, priority: P1, note: "Auth test user doc in ORKESTRA; RBAC seed exists" }
  T311: { orch: H2, status: assigned, priority: P1, batch: partner-ss-batch-1, note: "Docs/ORKESTRA_PANEL_SS_BATCH.md — 10 sayfa: dashboard(done kapı), tesis-konum, rezervasyonlar, takvim-fiyatlar, firma-fiyatlari, oda-yonetimi, oda-ozellikleri, otel-bilgileri, fotograflar, performans; route+view tabloda" }
  T312: { orch: H4, status: in_progress, priority: P2, note: "fe-user 6 sayfa SS path atandi; PNG bekliyor (firma/satis H6/H5)" }
  T313: { orch: H8, status: done, priority: P1 }
  T314: { orch: H10, status: assigned, priority: final }
  T320: { orch: H7, status: done }
  T321: { orch: H2, status: done, priority: P0 }
  T322: { orch: H3, status: in_progress, note: "platform-packages.css + mobile table-cards" }
  T323: { orch: H8, status: assigned }
  T324: { orch: H8, status: assigned }
  T325: { orch: H10, status: assigned }
  T326: { orch: H8, status: done, priority: P0, note: "Wave-I build verify .coord-build 0 error" }
  T327: { orch: H1, status: assigned, priority: P0, note: "OtelDetay full SS desktop+mobil" }
  T328: { orch: H1, status: assigned, priority: P1, note: "OtelListeleme empty-state polish" }
  T329: { orch: H1, status: assigned, priority: P1, note: "HaritaOteller cluster UX" }
  T330: { orch: H3, status: assigned, priority: P0, note: "Admin auth test user doc+seed path" }
  T331: { orch: H2, status: assigned, priority: P1, note: "Partner 47-page SS batch 1 (12 pages)" }
  T332: { orch: H6, status: assigned, priority: P1, note: "Firma remaining pages mobile+SS" }
  T333: { orch: H3, status: assigned, priority: P1, note: "FE-CTO batch approve path 10 pages" }
  T334: { orch: H3, status: assigned, priority: P1, note: "PlatformPackages T322 SS" }
  T335: { orch: H8, status: assigned, priority: P2, note: "T324 Faz2 payment spec only" }
  T336: { orch: H10, status: assigned, priority: P1, note: "K1 build gate audit doc" }
  T337: { orch: H10, status: assigned, priority: P1, note: "K2 security gate audit doc" }
  T338: { orch: H10, status: assigned, priority: P2, note: "K3 FE-CTO gate audit 6/151" }
  T339: { orch: H10, status: assigned, priority: P2, note: "Wave-I close snapshot ORKESTRA" }
  T340: { orch: H9, status: assigned, priority: P2, note: "Homepage A/B T369" }
  T341: { orch: H8, status: done, priority: P0, deliverable: Database/MigrationsSql/veri/migrationlar/20260526_seed_istanbul_ilce_oteller_tam.sql, note: "39 ilce otel+partner+oda+fiyat+kampanya+rezervasyon seed" }
  T342: { orch: H8, status: done, priority: P0, deliverable: Database/MigrationsSql/veri/migrationlar/20260526_seed_istanbul_ilce_medya_ozellik.sql, note: "ORK-IST/SEED medya, oda ozellikleri, FIYAT_INDIRIMLERI" }
  T343: { orch: H8, status: done, priority: P1, deliverable: Docs/ISTANBUL_ILCE_DEMO_KURULUM.md, note: "Partner login tablosu, test URL, sqlcmd" }
  T344: { orch: H8, status: done, priority: P1, note: "localdb otelturizm_2026db apply OK — 39 otel, 39 ilce, 2385 fiyat satiri" }
  T345: { orch: H8, status: done, priority: P1, note: "CTO kuyruk T341-T345 H8 guncelleme" }
  T346: { orch: H9, status: assigned, priority: P2, note: "AI search assist placeholder T370" }
  T347: { orch: H3, status: assigned, priority: P1, note: "Guest messaging oversight T364" }
  T348: { orch: H3, status: assigned, priority: P1, note: "Review moderation SLA queue" }
  T349: { orch: H8, status: done, priority: P0, note: "Build gate coord-build pass" }
  T350: { orch: H3, status: done, priority: P0, deliverable: RevenueCommandCenter, note: "/admin/gelir-merkezi|revenue-command-center; GetRevenueCommandCenterAsync; .build-t350 pass" }
  T351: { orch: H3, status: assigned, priority: P2, note: "Rate parity monitor" }
  T352: { orch: H3, status: assigned, priority: P1, note: "Real-time ops notification feed" }
  T353: { orch: H3, status: done, priority: P1, note: "SlowSqlMonitor /admin/slow-sql + SlowSql.cshtml + paneller/admin/slow-sql.mobile.css (Wave-II)" }
  T354: { orch: H7, status: assigned, priority: P1, note: "SecurityEvents.cshtml" }
  T355: { orch: H3, status: assigned, priority: P1, note: "UploadHistory.cshtml" }
  T356: { orch: H3, status: done, priority: P0, note: "Bulk hotel publish — /admin/oteller/toplu-yayin BulkUpdateHotelPublishStatus + Hotels.cshtml checkboxes; build .build-t356" }
  T357: { orch: H7, status: assigned, priority: P0, note: "FraudAlerts.cshtml" }
  T358: { orch: H3, status: assigned, priority: P1, note: "Multi-stage approval designer" }
  T359: { orch: H3, status: assigned, priority: P1, note: "Unified export center" }
  T360: { orch: H8, status: assigned, priority: P1, note: "Channel manager hub" }
  T361: { orch: H8, status: assigned, priority: P1, note: "API keys CRUD" }
  T362: { orch: H8, status: assigned, priority: P1, note: "Webhooks registry" }
  T363: { orch: H3, status: assigned, priority: P1, note: "Dynamic pricing admin read" }
  T364: { orch: H3, status: assigned, priority: P1, note: "Guest messaging oversight" }
  T365: { orch: H3, status: assigned, priority: P2, note: "Multi-property portfolio" }
  T366: { orch: H3, status: assigned, priority: P2, note: "White-label tenant" }
  T367: { orch: H3, status: assigned, priority: P2, note: "Scheduled reports" }
  T368: { orch: H8, status: assigned, priority: P2, note: "e-Fatura monitor" }
  T369: { orch: H9, status: assigned, priority: P3, note: "Homepage A/B admin" }
  T370: { orch: H9, status: assigned, priority: P3, note: "AI search assist config placeholder" }
  T371: { orch: H4, status: assigned, priority: P0, note: "U1 price drop alert — saved search + notify stub" }
  T372: { orch: H1, status: assigned, priority: P0, note: "U2 flash deal vitrin homepage+liste" }
  T373: { orch: H4, status: assigned, priority: P0, note: "U3 transparent total price checkout copy" }
  T374: { orch: H2, status: assigned, priority: P1, note: "U4 instant book partner toggle Wave-IV prep" }
  T375: { orch: H1, status: assigned, priority: P0, note: "U5 harita bbox cluster lazy load" }
  T376: { orch: H1, status: assigned, priority: P0, note: "U6 detay galeri fullscreen swipe lightbox" }
  T377: { orch: H3, status: assigned, priority: P1, note: "U7 review photo evidence moderation queue" }
  T378: { orch: H4, status: assigned, priority: P1, note: "U8 saved search alert email" }
  T379: { orch: H4, status: assigned, priority: P1, note: "U9 loyalty redeem at checkout" }
  T380: { orch: H1, status: assigned, priority: P2, note: "U10 compare 2-3 hotels side by side" }
  T381: { orch: H1, status: assigned, priority: P0, note: "U11 free cancel badge list card" }
  T382: { orch: H1, status: assigned, priority: P1, note: "U12 social proof viewers count ethical" }
  T383: { orch: H2, status: assigned, priority: P0, note: "Partner commission trend chart + payout ETA" }
  T384: { orch: H3, status: assigned, priority: P1, note: "Admin reservation occupancy heatmap" }
  T385: { orch: H3, status: assigned, priority: P1, note: "Campaign participation ROI attribution" }
  T386: { orch: H6, status: assigned, priority: P1, note: "Firma B2B bulk commission breakdown" }
  T387: { orch: H5, status: assigned, priority: P2, note: "Satis pipeline lead→offer→rez projection" }
  T388: { orch: H3, status: assigned, priority: P2, note: "Dynamic commission rule engine stub" }
  T389: { orch: H9, status: assigned, priority: P0, note: "JSON-LD Hotel Offer BreadcrumbList helper" }
  T390: { orch: H9, status: assigned, priority: P0, note: "39 ilce landing unique meta content" }
  T315: { orch: db-ork, status: done }
  T316: { orch: db-ork, status: done }
  T317: { orch: grup-03, status: done }
  T318: { orch: grup-04, status: done }
  T319: { orch: grup-05, status: done }
  T410: { orch: H11_finans_komisyon, status: done, priority: P0, note: "GetCommissionCollectionLedgerAsync paginated geo sort" }
  T411: { orch: H11_finans_komisyon, status: done, priority: P0, note: "Admin CommissionCollection.cshtml + /admin/komisyon-tahsilat" }
  T412: { orch: H11_finans_komisyon, status: done, priority: P0, note: "Excel/CSV export same filters" }
  T413: { orch: H11_finans_komisyon, status: done, priority: P1, note: "POST tahsilat bulk+single audit" }
  T414: { orch: H11_finans_komisyon, status: done, priority: P1, note: "Partner monthly commission + export" }
  T415: { orch: H11_finans_komisyon, status: done, priority: P0, note: "Migration PLATFORM_TAHSILAT_* columns" }
  T420: { orch: H4, status: assigned, priority: P0, note: "user-mobile-bundle.css" }
  T421: { orch: H4, status: assigned, priority: P0, note: "UserPanelController PageCssMobile all actions" }
  T422: { orch: H4, status: assigned, priority: P0, note: "11 user mobile.css MOBIL_TEK_EKRAN pass" }
  T435: { orch: H12_fatura_ork, status: assigned, priority: P0, note: "User Invoices mobile+download UX" }
  T437: { orch: H12_fatura_ork, status: assigned, priority: P0, note: "Partner GuestInvoices mobile upload" }
  T440: { orch: H13_i18n_ui, status: assigned, priority: P0, note: "SharedResources.resx scaffold" }
  T441: { orch: H13_i18n_ui, status: assigned, priority: P0, note: "Kamu layout i18n keys" }
  T445: { orch: H13_i18n_ui, status: assigned, priority: P1, note: "30 kamu strings wired" }
  T446: { orch: H9, status: assigned, priority: P0, note: "InternationalSeoService" }
  T447: { orch: H9, status: assigned, priority: P0, note: "Localized routes en/de" }
  T449: { orch: H9, status: assigned, priority: P0, note: "hreflang path-based" }
  T452: { orch: H14_email_ork, status: assigned, priority: P0, note: "Email master layout" }
  T453: { orch: H14_email_ork, status: assigned, priority: P0, note: "7 lang EmailTemplateService" }
  T454: { orch: H14_email_ork, status: assigned, priority: P1, note: "Fill missing email templates" }

  wave_vii:
    status: active
    wave_id: Wave-VII-20260526-komisyon-tahsilat
    orchestra: H11_finans_komisyon
    plan_doc: Docs/KOMISYON_TAHSILAT_MERKEZI_PLANI.md
    tasks: [T415, T410, T411, T412, T413, T414]

queue_active_parallel: [H11_finans_komisyon]
queue_wave_v_p0: []
queue_next_after_p0: [H4, H1, H2, H9, H10]
next_subagent_delegations:
  - task: T350
    owner: H3
    agent: fe-admin
    status: done
    scope: "Revenue Command Center — /admin/gelir-merkezi + RevenueCommandCenter.cshtml + GetRevenueCommandCenterAsync (30g GMV/komisyon/iptal/trend/otel liderleri/5651 başvuru)"
  - task: T356
    owner: H3
    agent: fe-admin
    status: done
    scope: "Bulk hotel publish — POST /admin/oteller/toplu-yayin BulkUpdateHotelPublishStatus; Hotels.cshtml checkboxes + audit hotel_bulk_*"
  - task: T398
    owner: H7
    agent: security
    status: done
    scope: "SalesPanel policy + ReturnUrl + satis@demo.otelturizm.local seed"
  - task: T410-T415
    owner: H11_finans_komisyon
    agent: finans-ork
    status: done
    scope: "Docs/KOMISYON_TAHSILAT_MERKEZI_PLANI.md — migration, ledger SQL, admin UI, Excel, partner export"
```
