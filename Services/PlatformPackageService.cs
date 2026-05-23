using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using otelturizmnew.Models.Paneller;
using otelturizmnew.Models.Paneller.Partner;
using otelturizmnew.Services.Abstractions;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;

namespace otelturizmnew.Services;

public sealed class PlatformPackageService : IPlatformPackageService
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private readonly string _connectionString;
    private readonly IPartnerService _partnerService;
    private readonly IAdminService _adminService;

    public PlatformPackageService(IConfiguration configuration, IPartnerService partnerService, IAdminService adminService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _partnerService = partnerService;
        _adminService = adminService;
    }

    public async Task<PartnerPlatformPackagesPageViewModel> GetPartnerCatalogAsync(long userId, long? hotelId, string? categoryCode, CancellationToken cancellationToken = default)
    {
        var dash = await _partnerService.GetDashboardAsync(userId, hotelId, cancellationToken: cancellationToken);
        var model = new PartnerPlatformPackagesPageViewModel
        {
            Shell = dash.Shell,
            SelectedHotelId = dash.Shell.SelectedHotelId ?? 0,
            SelectedHotelName = dash.Shell.SelectedHotelName ?? "—",
            CategoryFilter = categoryCode
        };

        if (model.SelectedHotelId <= 0)
        {
            return model;
        }

        await using var connection = await OpenAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "platform_paketler", cancellationToken))
        {
            model.TablesReady = false;
            return model;
        }

        await LoadHotelComplianceAsync(connection, model.SelectedHotelId, model, cancellationToken);
        model.Packages = await LoadPublishedPackagesAsync(connection, model.SelectedHotelId, model.HotelHas5661Installed, categoryCode, cancellationToken);
        model.Applications = await LoadPartnerApplicationsAsync(connection, userId, model.SelectedHotelId, cancellationToken);
        return model;
    }

    public async Task<PartnerPlatformPackageDetailPageViewModel?> GetPartnerPackageDetailAsync(long userId, long? hotelId, long packageId, CancellationToken cancellationToken = default)
    {
        if (packageId <= 0) return null;

        var dash = await _partnerService.GetDashboardAsync(userId, hotelId, cancellationToken: cancellationToken);
        var hotelIdValue = dash.Shell.SelectedHotelId ?? 0;
        if (hotelIdValue <= 0) return null;

        await using var connection = await OpenAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "platform_paketler", cancellationToken)) return null;

        var compliance = new PartnerPlatformPackagesPageViewModel { SelectedHotelId = hotelIdValue };
        await LoadHotelComplianceAsync(connection, hotelIdValue, compliance, cancellationToken);

        var packages = await LoadPublishedPackagesAsync(connection, hotelIdValue, compliance.HotelHas5661Installed, null, cancellationToken);
        var pkg = packages.FirstOrDefault(p => p.Id == packageId);
        if (pkg is null) return null;

        var eligible = IsEligible(pkg.TargetRule, compliance.HotelHas5661Installed);
        return new PartnerPlatformPackageDetailPageViewModel
        {
            Shell = dash.Shell,
            SelectedHotelId = hotelIdValue,
            SelectedHotelName = dash.Shell.SelectedHotelName ?? "—",
            Package = pkg,
            Eligible = eligible,
            EligibilityMessage = eligible
                ? "Bu paket için başvuru oluşturabilirsiniz."
                : "Otelinizde 5661 loglama sistemi kayıtlı; bu paket yalnızca 5661 kurulu olmayan tesisler içindir.",
            Form = new PartnerPlatformPackageApplicationFormModel
            {
                HotelId = hotelIdValue,
                PackageId = packageId
            }
        };
    }

    public async Task<(bool Success, string Message)> CreatePartnerApplicationAsync(long userId, PartnerPlatformPackageApplicationFormModel request, CancellationToken cancellationToken = default)
    {
        if (request.HotelId <= 0 || request.PackageId <= 0)
        {
            return (false, "Otel ve paket seçimi zorunludur.");
        }

        await using var connection = await OpenAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "partner_paket_basvurulari", cancellationToken))
        {
            return (false, "Paket satış tabloları bulunamadı. Migration uygulanmalı.");
        }

        var complianceProbe = new PartnerPlatformPackagesPageViewModel { SelectedHotelId = request.HotelId };
        await LoadHotelComplianceAsync(connection, request.HotelId, complianceProbe, cancellationToken);
        var hotel5661 = complianceProbe.HotelHas5661Installed || request.Hotel5661InstalledDeclaration;
        var packages = await LoadPublishedPackagesAsync(connection, request.HotelId, hotel5661, null, cancellationToken);
        var pkg = packages.FirstOrDefault(p => p.Id == request.PackageId);
        if (pkg is null)
        {
            return (false, "Paket bulunamadı veya bu otel için uygun değil.");
        }

        if (!IsEligible(pkg.TargetRule, hotel5661))
        {
            return (false, "5661 kurulu oteller için bu paket satışa kapalıdır.");
        }

        const string insertSql = @"
            INSERT INTO [dbo].[PARTNER_PAKET_BASVURULARI] (
                [PARTNER_KULLANICI_ID], [OTEL_ID], [PAKET_ID], [DURUM],
                [OTEL_5661_KURULU_BEYAN], [ILETISIM_AD], [ILETISIM_TELEFON], [ILETISIM_EPOSTA],
                [PARTNER_NOTU], [TEKLIF_TUTAR], [TEKLIF_PARA_BIRIMI]
            ) VALUES (
                @userId, @hotelId, @packageId, N'Beklemede',
                @decl5661, @name, @phone, @email,
                @note, @price, @currency
            );";

        await using var cmd = new SqlCommand(insertSql, connection);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@hotelId", request.HotelId);
        cmd.Parameters.AddWithValue("@packageId", request.PackageId);
        cmd.Parameters.AddWithValue("@decl5661", request.Hotel5661InstalledDeclaration);
        cmd.Parameters.AddWithValue("@name", (object?)request.ContactName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@phone", (object?)request.ContactPhone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@email", (object?)request.ContactEmail ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@note", (object?)request.PartnerNote ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@price", pkg.PriceAmount);
        cmd.Parameters.AddWithValue("@currency", pkg.Currency);

        await cmd.ExecuteNonQueryAsync(cancellationToken);

        await UpsertHotelComplianceDeclarationAsync(connection, request.HotelId, request.Hotel5661InstalledDeclaration, cancellationToken);

        return (true, $"{pkg.Title} için başvurunuz alındı. Admin onayından sonra aktivasyon yapılacaktır.");
    }

    public async Task<AdminPlatformPackagesPageViewModel> GetAdminPageAsync(string fullName, string email, string userRole, CancellationToken cancellationToken = default)
    {
        var listing = await _adminService.GetListingSubscriptionsAsync(fullName, email, userRole, cancellationToken);
        var model = new AdminPlatformPackagesPageViewModel { Shell = listing.Shell };

        await using var connection = await OpenAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "platform_paketler", cancellationToken))
        {
            model.TablesReady = false;
            model.Shell.PanelTitle = "Platform Paket Satışı";
            model.Shell.PanelSubtitle = "5651/5661 paket kataloğu ve partner başvuruları — migration bekleniyor.";
            return model;
        }

        model.Shell.PanelTitle = "Platform Paket Satışı";
        model.Shell.PanelSubtitle = "5651/5661 loglama paketleri, partner başvuru kuyruğu ve aktivasyon.";

        model.Packages = await LoadAllPackagesAdminAsync(connection, cancellationToken);
        model.Applications = await LoadAdminApplicationsAsync(connection, cancellationToken);
        model.Summary.PublishedPackageCount = model.Packages.Count;
        model.Summary.PendingCount = model.Applications.Count(a => a.Status == "Beklemede");
        model.Summary.ActiveCount = model.Applications.Count(a => a.Status is "Onaylandi" or "Aktif");
        return model;
    }

    public async Task<string> ExportAdminApplicationsCsvAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "partner_paket_basvurulari", cancellationToken))
        {
            return string.Join(';', new[] { Csv("mesaj"), Csv("Basvuru tablosu yok.") }) + Environment.NewLine;
        }

        const string sql = @"
            SELECT
                b.[ID], COALESCE(o.[OTEL_ADI], CONCAT(N'Otel #', b.[OTEL_ID])),
                COALESCE(k.[EPOSTA], N'-'), p.[PAKET_KODU], p.[BASLIK], b.[DURUM],
                b.[TEKLIF_TUTAR], b.[TEKLIF_PARA_BIRIMI], b.[OLUSTURULMA_UTC],
                COALESCE(b.[PARTNER_NOTU],''), COALESCE(b.[ADMIN_NOTU],''), b.[OTEL_5661_KURULU_BEYAN]
            FROM [dbo].[PARTNER_PAKET_BASVURULARI] b
            INNER JOIN [dbo].[PLATFORM_PAKETLER] p ON p.[ID] = b.[PAKET_ID]
            LEFT JOIN [dbo].[OTELLER] o ON o.[ID] = b.[OTEL_ID]
            LEFT JOIN [dbo].[KULLANICILAR] k ON k.[ID] = b.[PARTNER_KULLANICI_ID]
            ORDER BY
                CASE WHEN b.[DURUM] = N'Beklemede' THEN 0 ELSE 1 END,
                b.[OLUSTURULMA_UTC] DESC;";

        var sb = new StringBuilder();
        var inv = CultureInfo.InvariantCulture;
        sb.AppendLine(string.Join(';', new[]
        {
            Csv("basvuru_id"), Csv("otel"), Csv("partner_eposta"), Csv("paket_kodu"), Csv("paket"),
            Csv("durum"), Csv("tutar"), Csv("para_birimi"), Csv("olusturulma_utc"),
            Csv("partner_notu"), Csv("admin_notu"), Csv("otel_5661_beyan")
        }));

        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var amount = reader.IsDBNull(6) ? 0m : reader.GetDecimal(6);
            var currency = reader.IsDBNull(7) ? "TRY" : reader.GetString(7);
            var created = reader.IsDBNull(8) ? DateTime.UtcNow : reader.GetDateTime(8);
            sb.AppendLine(string.Join(';', new[]
            {
                Csv(reader.IsDBNull(0) ? string.Empty : Convert.ToInt64(reader.GetValue(0), inv).ToString(inv)),
                Csv(reader.IsDBNull(1) ? string.Empty : reader.GetString(1)),
                Csv(reader.IsDBNull(2) ? string.Empty : reader.GetString(2)),
                Csv(reader.IsDBNull(3) ? string.Empty : reader.GetString(3)),
                Csv(reader.IsDBNull(4) ? string.Empty : reader.GetString(4)),
                Csv(reader.IsDBNull(5) ? string.Empty : reader.GetString(5)),
                Csv(amount.ToString("0.##", inv)),
                Csv(currency),
                Csv(created.ToString("o", inv)),
                Csv(reader.IsDBNull(9) ? string.Empty : reader.GetString(9)),
                Csv(reader.IsDBNull(10) ? string.Empty : reader.GetString(10)),
                Csv(reader.IsDBNull(11) ? "false" : reader.GetBoolean(11).ToString(inv))
            }));
        }

        return sb.ToString();
    }

    public async Task<(bool Success, string Message)> ReviewApplicationAsync(long adminUserId, AdminPlatformPackageApplicationDecisionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ApplicationId <= 0)
        {
            return (false, "Başvuru seçilmedi.");
        }

        var action = (request.Action ?? string.Empty).Trim().ToLowerInvariant();
        string targetStatus;
        switch (action)
        {
            case "onayla":
            case "approve":
                targetStatus = "Onaylandi";
                break;
            case "aktif":
            case "activate":
                targetStatus = "Aktif";
                break;
            case "reddet":
            case "reject":
                targetStatus = "Reddedildi";
                break;
            case "iptal":
            case "cancel":
                targetStatus = "Iptal";
                break;
            default:
                return (false, "Geçersiz işlem. onayla, aktif, reddet veya iptal kullanın.");
        }

        await using var connection = await OpenAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "partner_paket_basvurulari", cancellationToken))
        {
            return (false, "Başvuru tablosu yok.");
        }

        var now = DateTime.UtcNow;
        var sql = targetStatus switch
        {
            "Aktif" => @"
                UPDATE [dbo].[PARTNER_PAKET_BASVURULARI]
                SET [DURUM] = @status,
                    [ADMIN_NOTU] = COALESCE(@note, [ADMIN_NOTU]),
                    [ONAYLAYAN_ADMIN_KULLANICI_ID] = @adminId,
                    [ONAY_TARIHI_UTC] = @now,
                    [AKTIF_BASLANGIC_UTC] = @now,
                    [AKTIF_BITIS_UTC] = DATEADD(month, 1, @now)
                WHERE [ID] = @id AND [DURUM] IN (N'Beklemede', N'Onaylandi');",
            "Onaylandi" => @"
                UPDATE [dbo].[PARTNER_PAKET_BASVURULARI]
                SET [DURUM] = @status,
                    [ADMIN_NOTU] = COALESCE(@note, [ADMIN_NOTU]),
                    [ONAYLAYAN_ADMIN_KULLANICI_ID] = @adminId,
                    [ONAY_TARIHI_UTC] = @now
                WHERE [ID] = @id AND [DURUM] = N'Beklemede';",
            _ => @"
                UPDATE [dbo].[PARTNER_PAKET_BASVURULARI]
                SET [DURUM] = @status,
                    [ADMIN_NOTU] = COALESCE(@note, [ADMIN_NOTU]),
                    [ONAYLAYAN_ADMIN_KULLANICI_ID] = @adminId
                WHERE [ID] = @id;"
        };

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@status", targetStatus);
        cmd.Parameters.AddWithValue("@note", (object?)request.AdminNote ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@adminId", adminUserId);
        cmd.Parameters.AddWithValue("@now", now);
        cmd.Parameters.AddWithValue("@id", request.ApplicationId);
        var rows = await cmd.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0
            ? (true, $"Başvuru güncellendi: {targetStatus}")
            : (false, "Başvuru bulunamadı veya durum geçişi uygun değil.");
    }

    private static bool IsEligible(string targetRule, bool hotel5661Installed) =>
        !string.Equals(targetRule, "OTEL_5661_YOK", StringComparison.OrdinalIgnoreCase) || !hotel5661Installed;

    private async Task LoadHotelComplianceAsync(SqlConnection connection, long hotelId, PartnerPlatformPackagesPageViewModel model, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "otel_uyum_durumlari", cancellationToken))
        {
            return;
        }

        const string sql = @"
            SELECT [LOG_5651_KURULU], [LOG_5661_KURULU]
            FROM [dbo].[OTEL_UYUM_DURUMLARI]
            WHERE [OTEL_ID] = @hotelId;";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            model.HotelHas5651Installed = !reader.IsDBNull(0) && reader.GetBoolean(0);
            model.HotelHas5661Installed = !reader.IsDBNull(1) && reader.GetBoolean(1);
        }
    }

    private async Task UpsertHotelComplianceDeclarationAsync(SqlConnection connection, long hotelId, bool log5661Installed, CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(connection, "otel_uyum_durumlari", cancellationToken))
        {
            return;
        }

        const string mergeSql = @"
            MERGE [dbo].[OTEL_UYUM_DURUMLARI] AS t
            USING (SELECT @hotelId AS OTEL_ID) AS s ON t.[OTEL_ID] = s.OTEL_ID
            WHEN MATCHED THEN
                UPDATE SET [LOG_5661_KURULU] = @flag, [GUNCELLEYEN_TIP] = N'PartnerBeyan', [GUNCELLEME_UTC] = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN
                INSERT ([OTEL_ID], [LOG_5661_KURULU], [GUNCELLEYEN_TIP])
                VALUES (@hotelId, @flag, N'PartnerBeyan');";

        await using var cmd = new SqlCommand(mergeSql, connection);
        cmd.Parameters.AddWithValue("@hotelId", hotelId);
        cmd.Parameters.AddWithValue("@flag", log5661Installed);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<List<PlatformPackageCardViewModel>> LoadPublishedPackagesAsync(
        SqlConnection connection,
        long hotelId,
        bool hotel5661Installed,
        string? categoryCode,
        CancellationToken cancellationToken)
    {
        var sql = @"
            SELECT p.[ID], p.[PAKET_KODU], k.[KOD], k.[BASLIK], p.[BASLIK], p.[KISA_ACIKLAMA], p.[DETAY_METIN],
                   p.[FIYAT_TUTAR], p.[PARA_BIRIMI], p.[FATURA_PERIYODU], p.[HEDEF_KURAL],
                   p.[KAPAK_GORSEL_URL], p.[GALERI_JSON], p.[OZELLIKLER_JSON], p.[SOZLESME_URL]
            FROM [dbo].[PLATFORM_PAKETLER] p
            INNER JOIN [dbo].[PLATFORM_PAKET_KATEGORILERI] k ON k.[ID] = p.[KATEGORI_ID]
            WHERE p.[DURUM] = N'Yayinda' AND k.[AKTIF_MI] = 1";

        if (!string.IsNullOrWhiteSpace(categoryCode))
        {
            sql += " AND k.[KOD] = @cat";
        }

        sql += " ORDER BY p.[SIRA] ASC, p.[ID] ASC;";

        await using var cmd = new SqlCommand(sql, connection);
        if (!string.IsNullOrWhiteSpace(categoryCode))
        {
            cmd.Parameters.AddWithValue("@cat", categoryCode.Trim());
        }

        var list = new List<PlatformPackageCardViewModel>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var targetRule = reader.IsDBNull(10) ? "HER_OTEL" : reader.GetString(10);
            if (!IsEligible(targetRule, hotel5661Installed))
            {
                continue;
            }

            list.Add(MapPackageRow(reader));
        }

        return list;
    }

    private async Task<List<PlatformPackageCardViewModel>> LoadAllPackagesAdminAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT p.[ID], p.[PAKET_KODU], k.[KOD], k.[BASLIK], p.[BASLIK], p.[KISA_ACIKLAMA], p.[DETAY_METIN],
                   p.[FIYAT_TUTAR], p.[PARA_BIRIMI], p.[FATURA_PERIYODU], p.[HEDEF_KURAL],
                   p.[KAPAK_GORSEL_URL], p.[GALERI_JSON], p.[OZELLIKLER_JSON], p.[SOZLESME_URL]
            FROM [dbo].[PLATFORM_PAKETLER] p
            INNER JOIN [dbo].[PLATFORM_PAKET_KATEGORILERI] k ON k.[ID] = p.[KATEGORI_ID]
            ORDER BY p.[SIRA] ASC, p.[ID] ASC;";

        var list = new List<PlatformPackageCardViewModel>();
        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapPackageRow(reader));
        }

        return list;
    }

    private static PlatformPackageCardViewModel MapPackageRow(SqlDataReader reader)
    {
        var amount = reader.IsDBNull(7) ? 0m : reader.GetDecimal(7);
        var currency = reader.IsDBNull(8) ? "TRY" : reader.GetString(8);
        var period = reader.IsDBNull(9) ? "Aylik" : reader.GetString(9);
        var targetRule = reader.IsDBNull(10) ? "HER_OTEL" : reader.GetString(10);

        return new PlatformPackageCardViewModel
        {
            Id = Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture),
            PackageCode = reader.IsDBNull(1) ? "" : reader.GetString(1),
            CategoryCode = reader.IsDBNull(2) ? "" : reader.GetString(2),
            CategoryTitle = reader.IsDBNull(3) ? "" : reader.GetString(3),
            Title = reader.IsDBNull(4) ? "" : reader.GetString(4),
            ShortDescription = reader.IsDBNull(5) ? "" : reader.GetString(5),
            DetailText = reader.IsDBNull(6) ? "" : reader.GetString(6),
            PriceAmount = amount,
            Currency = currency,
            BillingPeriod = period,
            TargetRule = targetRule,
            CoverImageUrl = reader.IsDBNull(11) ? "" : reader.GetString(11),
            GalleryUrls = ParseJsonStringList(reader.IsDBNull(12) ? null : reader.GetString(12)),
            Features = ParseJsonStringList(reader.IsDBNull(13) ? null : reader.GetString(13)),
            ContractUrl = reader.IsDBNull(14) ? null : reader.GetString(14),
            PriceText = FormatPrice(amount, currency, period),
            TargetRuleBadge = targetRule switch
            {
                "OTEL_5661_YOK" => "5661 kurulu olmayan oteller",
                _ => "Tüm oteller"
            }
        };
    }

    private async Task<List<PartnerPlatformPackageApplicationRowViewModel>> LoadPartnerApplicationsAsync(
        SqlConnection connection, long userId, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (80) b.[ID], p.[BASLIK], b.[DURUM], b.[TEKLIF_TUTAR], b.[TEKLIF_PARA_BIRIMI], b.[OLUSTURULMA_UTC], COALESCE(b.[PARTNER_NOTU],'')
            FROM [dbo].[PARTNER_PAKET_BASVURULARI] b
            INNER JOIN [dbo].[PLATFORM_PAKETLER] p ON p.[ID] = b.[PAKET_ID]
            WHERE b.[PARTNER_KULLANICI_ID] = @userId AND b.[OTEL_ID] = @hotelId
            ORDER BY b.[OLUSTURULMA_UTC] DESC;";

        var list = new List<PartnerPlatformPackageApplicationRowViewModel>();
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@hotelId", hotelId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var amount = reader.IsDBNull(3) ? 0m : reader.GetDecimal(3);
            var currency = reader.IsDBNull(4) ? "TRY" : reader.GetString(4);
            var created = reader.IsDBNull(5) ? DateTime.UtcNow : reader.GetDateTime(5);
            list.Add(new PartnerPlatformPackageApplicationRowViewModel
            {
                Id = Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture),
                PackageTitle = reader.IsDBNull(1) ? "-" : reader.GetString(1),
                Status = reader.IsDBNull(2) ? "-" : reader.GetString(2),
                PriceText = FormatPrice(amount, currency, "Aylik"),
                CreatedText = created.ToLocalTime().ToString("dd.MM.yyyy HH:mm", Tr),
                Note = reader.IsDBNull(6) ? "" : reader.GetString(6)
            });
        }

        return list;
    }

    private async Task<List<AdminPlatformPackageApplicationRowViewModel>> LoadAdminApplicationsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT TOP (200)
                b.[ID], COALESCE(o.[OTEL_ADI], CONCAT(N'Otel #', b.[OTEL_ID])),
                COALESCE(k.[EPOSTA], N'-'), p.[BASLIK], b.[DURUM],
                b.[TEKLIF_TUTAR], b.[TEKLIF_PARA_BIRIMI], b.[OLUSTURULMA_UTC],
                COALESCE(b.[PARTNER_NOTU],''), COALESCE(b.[ADMIN_NOTU],''), b.[OTEL_5661_KURULU_BEYAN]
            FROM [dbo].[PARTNER_PAKET_BASVURULARI] b
            INNER JOIN [dbo].[PLATFORM_PAKETLER] p ON p.[ID] = b.[PAKET_ID]
            LEFT JOIN [dbo].[OTELLER] o ON o.[ID] = b.[OTEL_ID]
            LEFT JOIN [dbo].[KULLANICILAR] k ON k.[ID] = b.[PARTNER_KULLANICI_ID]
            ORDER BY
                CASE WHEN b.[DURUM] = N'Beklemede' THEN 0 ELSE 1 END,
                b.[OLUSTURULMA_UTC] DESC;";

        var list = new List<AdminPlatformPackageApplicationRowViewModel>();
        if (!await TableExistsAsync(connection, "oteller", cancellationToken))
        {
            return list;
        }

        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var amount = reader.IsDBNull(5) ? 0m : reader.GetDecimal(5);
            var currency = reader.IsDBNull(6) ? "TRY" : reader.GetString(6);
            var created = reader.IsDBNull(7) ? DateTime.UtcNow : reader.GetDateTime(7);
            list.Add(new AdminPlatformPackageApplicationRowViewModel
            {
                Id = Convert.ToInt64(reader.GetValue(0), CultureInfo.InvariantCulture),
                HotelName = reader.IsDBNull(1) ? "-" : reader.GetString(1),
                PartnerEmail = reader.IsDBNull(2) ? "-" : reader.GetString(2),
                PackageTitle = reader.IsDBNull(3) ? "-" : reader.GetString(3),
                Status = reader.IsDBNull(4) ? "-" : reader.GetString(4),
                PriceText = FormatPrice(amount, currency, "Aylik"),
                CreatedText = created.ToLocalTime().ToString("dd.MM.yyyy HH:mm", Tr),
                PartnerNote = reader.IsDBNull(8) ? "" : reader.GetString(8),
                AdminNote = reader.IsDBNull(9) ? "" : reader.GetString(9),
                Hotel5661Declared = !reader.IsDBNull(10) && reader.GetBoolean(10)
            });
        }

        return list;
    }

    private static string Csv(string? value)
    {
        var v = value ?? string.Empty;
        v = v.Replace("\"", "\"\"", StringComparison.Ordinal);
        return $"\"{v}\"";
    }

    private static string FormatPrice(decimal amount, string currency, string period)
    {
        var money = string.Format(Tr, "{0:N0} {1}", amount, currency);
        var periodText = string.Equals(period, "Yillik", StringComparison.OrdinalIgnoreCase) ? "/ yıl" : "/ ay";
        return money + periodText;
    }

    private static List<string> ParseJsonStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static async Task<bool> TableExistsAsync(SqlConnection connection, string tableName, CancellationToken cancellationToken)
    {
        const string sql = "SELECT 1 FROM sys.tables WHERE name = @name AND schema_id = SCHEMA_ID('dbo');";
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@name", tableName);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }
}
