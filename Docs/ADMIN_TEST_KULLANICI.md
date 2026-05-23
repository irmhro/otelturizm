# Admin panel — test kullanıcı ve seed yolu (T330)

Son güncelleme: **2026-05-23**  
Orkestratör: **H3** (`fe-admin`)

## Özet

Yerel admin SS, RBAC ve FE-CTO batch için **idempotent** demo hesap ve uygulama sırası tek sayfada toplanır. Yetki tanımları ayrı seed’de kalır; bu dosya yalnızca **giriş yapılabilir** admin kullanıcı + `platform_admin_full` rol bağlantısını ekler.

## Kimlik bilgileri (yalnızca local / dev)

| Alan | Değer |
|------|--------|
| Giriş URL | `http://127.0.0.1:5103/admin-giris` |
| E-posta | `ork-demo-admin@otelturizm.local` |
| Şifre | `Demo123!` |
| `KULLANICILAR.ROL` | `admin` |
| RBAC rol | `platform_admin_full` |

**Üretimde bu hesabı kullanmayın.** Seed yalnızca geliştirme ve screenshot otomasyonu içindir.

## Migration dosyaları (sıra)

| Sıra | Dosya | Açıklama |
|------|--------|----------|
| 1 | `Database/MigrationsSql/veri/migrationlar/20260522_seed_admin_yetkiler.sql` | `admin.*` yetkileri + `platform_admin_full` rol + rol–yetki eşlemesi |
| 2 | `Database/MigrationsSql/veri/migrationlar/20260523_seed_admin_demo_kullanici.sql` | Demo admin kullanıcı + `ADMIN_KULLANICI_ROLLER` (idempotent) |
| 3 (opsiyonel) | `Database/MigrationsSql/veri/migrationlar/20260525_seed_platform_paket_5651_5661.sql` | Platform paket kataloğu (admin PlatformPackages sayfası içerik) |

Genel runner sırası: `Database/MigrationsSql/README.md` → tablo → constraints → `veri/migrationlar/*.sql` (dosya adına göre).

## Uygulama (yerel)

**Otomatik (uygulama açılışı):** `SqlMigrationRunner` tüm `veri/migrationlar` betiklerini sırayla çalıştırır.

**Manuel (sqlcmd):**

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d otelturizm_2026db -i "Database\MigrationsSql\veri\migrationlar\20260522_seed_admin_yetkiler.sql"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d otelturizm_2026db -i "Database\MigrationsSql\veri\migrationlar\20260523_seed_admin_demo_kullanici.sql"
```

**PowerShell script:** `tools/Db/apply_local_database.ps1` (tüm migration seti).

## Doğrulama

```sql
SELECT u.[ID], u.[EPOSTA], u.[ROL], r.[ROL_CODE], r.[ACTIVE]
FROM [dbo].[KULLANICILAR] u
LEFT JOIN [dbo].[ADMIN_KULLANICI_ROLLER] r ON r.[ADMIN_KULLANICI_ID] = u.[ID]
WHERE u.[EPOSTA] = N'ork-demo-admin@otelturizm.local';
```

Beklenen: `ROL = admin`, `ROL_CODE = platform_admin_full`, `ACTIVE = 1`.

Giriş sonrası claim: `accountType=admin` veya `userRole=admin` (`AdminPanelController.CanAccessAdminPanel`).

## İlgili dokümanlar

- SS batch ve mobil tablo: `Docs/ORKESTRA_PANEL_SS_BATCH.md`
- FE-CTO onay yolu (10 sayfa): `Docs/FE_CTO_ADMIN_BATCH_T333.md`
- Frontend envanter: `FRONTEND_ORKESTRATOR_PLAN.md` → `fe-admin`

## Bloker çözümü

| Önceki durum | T330 sonrası |
|--------------|--------------|
| RBAC seed var, kullanıcı yok → admin-giris başarısız | Demo admin + rol bağlantısı seed ile giriş mümkün |
| Manuel `ADMIN_KULLANICI_ROLLER` INSERT | Seed tekrar çalıştırılabilir; mevcut e-posta korunur |

Partner demo (`ork-demo-partner@otelturizm.local`) admin paneli için **geçerli değildir**.
