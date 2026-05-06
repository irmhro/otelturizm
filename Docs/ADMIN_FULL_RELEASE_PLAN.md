# Admin Panel — Tam Sürüm Planı (2026)

Bu belge, DB tabanlı RBAC, shell/UI, ticari raporlar ve sistem sayfalarında **eksiksiz kapanış** için faz bazlı kontrol listesidir. Teknik kaynak: `Controllers/Paneller/Admin/AdminPanelController.cs`, `Views/Paneller/Admin/_AdminSidebar.cshtml`, `Database/MigrationsSql/20260504_admin_rbac.sql`.

## Faz A — Kimlik doğrulama ve RBAC

| Madde | Durum |
|-------|--------|
| `admin_*` tabloları + seed roller | Migration ile |
| `HasPermissionAsync` + menüde `Can(...)` | Uygulandı |
| **Tüm** admin GET/POST uçlarında endpoint guard | Sürekli genişletilir (`RequirePermissionOrForbidAsync`) |
| Yeni `RenderSectionAsync` anahtarı → izin eşlemesi | Zorunlu; boş eşleme `Forbid` |

## Faz B — Shell (sidebar / header / footer)

| Madde | Durum |
|-------|--------|
| `panel-admin-shell.css` — Tabler ile çakışma | İzlendi |
| Mobil alt bar + içerik padding | İzlendi |

## Faz C — Menü ↔ route ↔ view

| Madde | Durum |
|-------|--------|
| Sidebar’da her link için uygun `admin.*` izni | `Can()` ile |
| Kırık link / eksik view | Envanter + düzeltme |

## Faz D — Ticari operasyon ve komisyon

| Madde | Durum |
|-------|--------|
| Komisyon kuralları (`Commissions`) | Mevcut |
| Gelir/komisyon raporu (`Reports`) + rezervasyon snapshot | Mevcut |
| Aylık CSV export | `/admin/raporlar/aylik-ciro-komisyon.csv` |

## Faz E — Otel yaşam döngüsü ve audit

| Madde | Durum |
|-------|--------|
| Onay merkezi / bekleyen-yayında akış | Ekranlar mevcut |
| Kritik otel aksiyonlarında audit | `IAuditLogService` |

## Faz F — Sistem ve iletişim

| Madde | Durum |
|-------|--------|
| Sistem sağlığı, e-posta kuyruk, rate limit, loglar | İzin kodları tanımlı |
| Sitemap, ayar monitörü | `admin.sitemap`, `admin.settings_monitor` |

## Faz G — Test

- Kısıtlı rol ile menü görünürlüğü + doğrudan URL → 403.
- Finans rolü: rapor/komisyon; Ops: otel/rezervasyon; Content: blog/SSS.

## Operasyon notu

Canlıda `schema_migrations` / RBAC scriptleri uygulanmadan kısıtlı roller beklenen davranışı göstermez; deploy öncesi yedek zorunlu.
