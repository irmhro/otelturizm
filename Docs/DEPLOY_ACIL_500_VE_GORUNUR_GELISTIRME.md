# ACİL DEPLOY — HTTP 500 ve görünür geliştirme

**Tarih:** 2026-05-24  
**Dalga:** #073 deploy-gap + görünür FE (otel listesi)  
**Durum:** Kod repoda hazır; **canlıda değişiklik yalnızca tam Release publish + app pool recycle sonrası görünür.**

---

## 1. Neden sitede hiçbir şey değişmiyor?

| Durum | Sonuç |
|-------|--------|
| Sunucuya **sadece `.cshtml`** kopyalandı, **DLL publish edilmedi** | Razor/i18n (`SharedLocalizer`, `SharedResources`) eski derleme ile uyumsuz → **500** veya eski görünüm |
| IIS’te **eski `bin` + yeni `Views`** karışık | Aynı: runtime/derleme hatası, tutarsız sayfa |
| `ASPNETCORE_ENVIRONMENT=Development` canlıda | Runtime compilation + i18n kaynakları → **500** (`CS0234 Resources` vb.) |
| Tarayıcı / CDN önbelleği | CSS/JS güncellemesi görünmez (Ctrl+F5 gerekir) |
| SQL seed’ler canlıda **çalıştırılmadı** | Demo oteller / sözleşme linkleri eksik kalır (500’ün ana nedeni değil) |

**Özet:** Orkestra #053–#072 dalgalarının çoğu **yalnızca bu repoda**. Eski build deploy edildiyse kullanıcı **sıfır iyileşme** görür; `/oteller` 500 devam eder.

**500 kök neden (düzeltildi #063/#065):** `_Layout` + `_ViewImports` → `IStringLocalizer<SharedResources>`. Ana sayfa `Layout=null` olduğu için 200; layout kullanan sayfalar 500.

Detay: [`CANLI_500_KOK_NEDEN.md`](CANLI_500_KOK_NEDEN.md)

---

## 2. PowerShell — tam Release publish (D:\otelturizm)

**Önkoşul:** Uygulama dosyaları kilitliyorsa IIS’te siteyi durdurun veya app pool’u durdurun.

```powershell
Set-Location "D:\otelturizm"

# 1) Derleme doğrulama (0 hata)
dotnet build "D:\otelturizm\otelturizm.csproj" -c Release -o ".coord-build-deploy-ready"

# 2) Canlıya gidecek paket
dotnet publish "D:\otelturizm\otelturizm.csproj" -c Release -o "D:\otelturizm\publish\deploy-ready"

# 3) IIS: publish\deploy-ready içeriğini site fiziksel yoluna KOPYALAYIN (Views + wwwroot + bin hepsi)
# 4) Application Pool → Recycle (veya Stop → Start)
```

**Yapmayın:**

- Sadece `Views\Oteller\OtelListeleme.cshtml` atmak  
- Eski `bin\otelturizm.dll` bırakıp yeni view koymak  
- Development ortam değişkeni ile çalıştırmak  

**Alternatif çıktı klasörü (koordinasyon):** `.coord-build-deploy-ready` — build doğrulama; canlı paket için `publish\deploy-ready` kullanın.

---

## 3. Ortam — Production zorunlu

| Ayar | Değer |
|------|--------|
| `ASPNETCORE_ENVIRONMENT` | **Production** |
| `DevelopmentGate:Enabled` | **false** (`appsettings.Production.json` veya sunucu override) |
| Connection string | Sunucuda `ConnectionStrings__DefaultConnection` (repoya şifre yazmayın) |

---

## 4. SQL — yedekten sonra (sıra önemli)

**Önce:** SQL Server **full backup** (zorunlu).

| Sıra | Dosya | Amaç |
|------|--------|------|
| 1 | `Database\MigrationsSql\veri\migrationlar\20260524_seed_kullanici_yasal_sozlesmeler.sql` | Kullanıcı KVKK + kullanım koşulları (footer yasal) |
| 2 | `Database\MigrationsSql\veri\migrationlar\20260524_seed_platform_sozlesmeler.sql` | Platform sözleşme içerikleri |
| 3 | `Database\MigrationsSql\20260523_ensure_demo_hotels_published.sql` | Demo/yayın otellerin `Yayında` + `Onaylandi` |
| 4 | (gerekirse) `Database\MigrationsSql\20260526_fix_yayin_onay_unicode.sql` | Yayın durumu Unicode düzeltmesi |

**Örnek (sqlcmd, sunucuya göre düzenleyin):**

```powershell
sqlcmd -S "SUNUCU" -d "VERITABANI" -U "kullanici" -P "sifre" -I -f 65001 `
  -i "D:\otelturizm\Database\MigrationsSql\veri\migrationlar\20260524_seed_kullanici_yasal_sozlesmeler.sql"

sqlcmd -S "SUNUCU" -d "VERITABANI" -U "kullanici" -P "sifre" -I -f 65001 `
  -i "D:\otelturizm\Database\MigrationsSql\veri\migrationlar\20260524_seed_platform_sozlesmeler.sql"

sqlcmd -S "SUNUCU" -d "VERITABANI" -U "kullanici" -P "sifre" -I -f 65001 `
  -i "D:\otelturizm\Database\MigrationsSql\20260523_ensure_demo_hotels_published.sql"
```

---

## 5. Smoke test (200 beklenir)

Deploy + recycle + **Ctrl+F5** sonrası:

| # | URL | Beklenen |
|---|-----|----------|
| 1 | `https://otelturizm.com/` | 200 |
| 2 | `https://otelturizm.com/oteller` | 200 — liste açılır; **sonuç rozeti** (tesis sayısı) görünür |
| 3 | `https://otelturizm.com/kullanici-giris?sekme=kayit` | 200 |
| 4 | `https://otelturizm.com/partner-giris` | 200 |
| 5 | `https://otelturizm.com/Oteller` | 301 → `/oteller` |
| 6 | `https://otelturizm.com/kurumsal/kullanim-kosullari` (veya footer yasal link) | 200 (seed sonrası) |

**Görünür FE (#073):** `/oteller` üst bölümde **“X tesis”** rozeti; boş listede **“Tüm otelleri göster”** birincil CTA → `/oteller`.

---

## 6. Önbellek

- Tarayıcı: **Ctrl+F5** (hard refresh)  
- Cloudflare / reverse proxy varsa: ilgili path purge (`/oteller`, `/assets/css/*`)  
- IIS static content cache: recycle sonrası yeni `asp-append-version` query string’leri yüklenir  

---

## 7. Hata sürerse

1. `App_Data/logs/app-*.json` — son satırlarda `CompilationFailedException`, `SharedResources`, SQL  
2. Sunucuda `ASPNETCORE_ENVIRONMENT` gerçekten **Production** mı?  
3. Site klasöründe **tek** publish seti var mı (karışık `Views` + eski `bin` yok mu?)  

---

**Bu dosya canlı operasyon içindir.** Git push otomatik deploy yapmaz; sunucuda publish şarttır.
