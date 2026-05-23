# Canlı HTTP 500 — Kök Neden ve Deploy Checklist

**Tarih:** 2026-05-24  
**Durum:** P0 giderildi (kod); canlıya tam publish gerekli.

## Özet

| URL | Canlı (önce) | Kök neden |
|-----|--------------|-----------|
| `/` | 200 | `Anasayfa.cshtml` → `Layout = null` ( `_Layout` yok ) |
| `/oteller`, `/kullanici-giris`, `/partner-giris`, … | **500** | `_Layout` + Razor derleme / i18n (`SharedLocalizer`) |

Ana sayfa çalışıp diğer sayfaların 500 vermesi, hatanın controller’da değil **ortak layout / view pipeline**’da olduğunu gösterir.

## Kök neden (teknik)

1. **Razor Runtime Compilation + i18n (Wave-XVIII)**  
   `AddViewLocalization` ve `_ViewImports` içinde `IStringLocalizer<SharedResources>` eklendi. Sunucu `ASPNETCORE_ENVIRONMENT=Development` ile veya eski DLL + yeni `.cshtml` ile çalışınca runtime derleme şu hatayı üretiyordu:
   - `CS0234: 'Resources' ad alanı 'otelturizmnew' içinde yok`
   - `OtelListeleme.cshtml` için eksik using / eski view-model DLL uyumsuzluğu

2. **`_Layout.cshtml` içinde `GetActiveDraftAsync`**  
   Her layout’lu sayfada SQL çağrısı; tablo/ bağlantı hatasında sayfa 500 olabilirdi (fail-safe try/catch eklendi).

3. **`/Oteller` büyük harf**  
   Route’lar küçük harf (`/oteller`); büyük harf eşleşmez (301 redirect eklendi).

## Yapılan kod düzeltmeleri (#063)

- `Program.cs`: Runtime compilation yalnızca local Development; `ResourcesPath`; `/Oteller` → `/oteller` 301
- `RoutePrefixRequestCultureProvider`: null-safe, `tr-TR` fallback
- `_Layout.cshtml`: draft SQL try/catch
- `otelturizm.csproj`: `PreserveCompilationContext`, `MvcRazorCompileOnPublish`
- `appsettings.Production.json`: connection string placeholder + `DevelopmentGate:Enabled: false`

## Deploy checklist (canlı)

1. **Yedek** — SQL full backup (zorunlu).
2. **Ortam değişkeni** — `ASPNETCORE_ENVIRONMENT=Production` (Development olmamalı).
3. **Connection string** — Sunucuda `appsettings.Production.json` veya ortam değişkeni:
   - `ConnectionStrings__DefaultConnection`
   - Gerçek şifreyi repoya commit etmeyin.
4. **Tam publish** — Sadece `.cshtml` kopyalamayın; Release build + publish:
   ```powershell
   dotnet publish "D:\otelturizm\otelturizm.csproj" -c Release -o .\publish\prod-fix
   ```
5. **IIS / site** — App pool recycle; eski `Views` + yeni `dll` karışımını bırakmayın.
6. **DevelopmentGate** — Canlıda `DevelopmentGate:Enabled` = `false` (appsettings veya sunucu override).
7. **Doğrulama** (200 beklenir):
   - `https://otelturizm.com/`
   - `https://otelturizm.com/oteller`
   - `https://otelturizm.com/kullanici-giris?sekme=kayit`
   - `https://otelturizm.com/partner-giris`
   - `https://otelturizm.com/oteller/orkestra-taksim-suites`
   - `https://otelturizm.com/Oteller` → 301 → `/oteller`
8. **Log** — Hata sürerse `App_Data/logs/app-*.json` son satırlarda `CompilationFailedException` veya SQL arayın.

## Not

Arka plan servisleri (`user_favorite_price_alert_jobs` vb.) eksik tabloda log yazar; **sayfa 500’ün ana nedeni değildir** — migration uygulanmalı ama P0 sayfa açılışını bloklamaz.
