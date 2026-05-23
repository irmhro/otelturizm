# Admin Panel — Ajan Grubu

**Grup ID:** `08`  
**Charter:** [docs/agent-gruplari/08-admin-panel.md](docs/agent-gruplari/08-admin-panel.md)  
**Bağımlılık:** `03` Services ✅ · `05` Views ✅ · `06` Frontend 🔄

## Ajanlar

| Ajan | Sorumluluk |
|------|------------|
| `admin-db` | RBAC SQL, admin servis şeması |
| `admin-razor` | `Views/Paneller/Admin/**` |
| `admin-css` | `wwwroot/assets/css/paneller/admin/**` |
| `dual-cto` | Mobil + sidebar parity |

## Grup ID → dosya

| Grup ID | Dosya |
|---------|-------|
| **08** | `Controllers/Paneller/Admin/AdminPanelController.cs` |
| **08** | `Views/Paneller/Admin/**` |
| **08** | `wwwroot/assets/css/paneller/admin/**`, `panel-admin-shell*` |
| **08** | `Models/Paneller/Admin/**` |

## Checklist

| # | Madde | Durum |
|---|-------|-------|
| 1 | Sidebar akordeon / aktif sayfa | ✅ |
| 2 | RBAC menü `Can()` | ✅ |
| 3 | Mobil alt nav | ✅ |
| 4 | Konaklama yönetimi tam akış | 🔄 |
| 5 | Placeholder → tablo standardı | 🔄 |

## İlgili planlar

- [Docs/ADMIN_PANEL_FULL_PLAN.md](Docs/ADMIN_PANEL_FULL_PLAN.md)
- [AGENT_GRUPLARI_MASTER.md](AGENT_GRUPLARI_MASTER.md)
