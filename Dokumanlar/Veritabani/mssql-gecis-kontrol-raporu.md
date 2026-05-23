# MSSQL Gecis Kontrol Raporu

Tarih: 2026-04-16

## Genel Sonuc
- Konfigurasyon katmani MSSQL/LocalDB'ye alinmis durumda.
- Ana web projesinde aktif `MySqlConnector` referansi kalmadi.
- CSS/JS tarafinda `mysql`, `laragon`, `heidi` kalintisi bulunmadi.
- Ancak aktif runtime servislerinde hala MSSQL'e donusturulmemis cok sayida MySQL sorgu kalibi bulunuyor.
- Bu nedenle gecis durumu su an icin `%100 tamamlandi` olarak kabul edilmemelidir.

## Temizlenenler
- `Data/SqlMigrationRunner.cs` icindeki MySQL odakli calisma tarzi temizlendi.
- Gelistirme ortami dokumanlari `MSSQLLocalDB` ve SQL Server araclarina gore guncellendi.
- `HotelService.cs` icindeki eski MySQL yorum referansi temizlendi.

## Temiz Olan Katmanlar
- `appsettings.json` -> `SqlServer`
- `appsettings.Development.json` -> `(localdb)\\MSSQLLocalDB`
- `otelturizm.csproj` -> `Microsoft.Data.SqlClient`
- `wwwroot` altindaki CSS/JS -> MySQL/Laragon/Heidi kalintisi yok

## Aktif Runtime'da Hala MySQL Sozdizimi Bulunan Kritik Servisler
- `Services/PartnerService.cs` -> 63 bulgu
- `Services/HotelService.cs` -> 45 bulgu
- `Services/AuthService.cs` -> 39 bulgu
- `Services/UserPanelService.cs` -> 33 bulgu
- `Services/SalesService.cs` -> 29 bulgu
- `Services/AdminService.cs` -> 24 bulgu
- `Services/AdminHotelManagementService.cs` -> 20 bulgu
- `Services/FirmaService.cs` -> 15 bulgu
- `Services/CampaignService.cs` -> 12 bulgu
- `Services/ReservationDraftService.cs` -> 6 bulgu
- `Services/UserFavoriteService.cs` -> 6 bulgu
- `Services/MessageCenterService.cs` -> 5 bulgu
- `Services/PublicReservationService.cs` -> 4 bulgu
- `Services/HotelPricingReadService.cs` -> 1 bulgu

## Runtime'da Donusmesi Gereken Baslica MySQL Kaliplari
- `LIMIT`
- `NOW()`
- `CURDATE()`
- `DATE_FORMAT(...)`
- `IFNULL(...)`
- `GROUP_CONCAT(...)`
- `DATE_ADD(...)`
- `DATE_SUB(...)`
- `LAST_INSERT_ID()`
- `SUBSTRING_INDEX(...)`

## Tarihsel / Tool Katmaninda Kalanlar
Bu dosyalar ana web projesine derleme olarak dahil edilmiyor, ancak tarihsel amacli MySQL referansi tasiyor:
- `tools/InventoryReset/InventoryReset.csproj`
- `tools/InventoryReset/Program.cs`
- `tools/MySqlToMssqlMigrator/MySqlToMssqlMigrator.csproj`
- `tools/MySqlToMssqlMigrator/Program.cs`
- `tools/MySqlToMssqlMigrator/README.md`

## Derleme Dogrulamasi
- `dotnet build --no-restore` basarili
- `0 hata`
- `0 uyari`

## Net Karar
- Kod tabani `MSSQL'e yonlendirilmis durumda`
- Ama `aktif runtime SQL sorgulari toplu refactor` bekliyor
- Bu tamamlanmadan `MSSQL donusumu %100 bitti` denmemeli
