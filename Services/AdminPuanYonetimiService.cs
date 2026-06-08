using System.Globalization;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using otelturizmnew.Models.Paneller.Admin;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class AdminPuanYonetimiService : IAdminPuanYonetimiService
{
    private readonly string _connectionString;
    private readonly IAdminRbacService _adminRbacService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHotelPointsService _hotelPointsService;

    public AdminPuanYonetimiService(
        IConfiguration configuration,
        IAdminRbacService adminRbacService,
        IHttpContextAccessor httpContextAccessor,
        IHotelPointsService hotelPointsService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _adminRbacService = adminRbacService;
        _httpContextAccessor = httpContextAccessor;
        _hotelPointsService = hotelPointsService;
    }

    public async Task<AdminPuanYonetimiPageViewModel> GetPageAsync(
        string fullName,
        string email,
        string userRole,
        string? tab = null,
        long? editRuleId = null,
        long? filterUserId = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var model = new AdminPuanYonetimiPageViewModel
        {
            Shell = await BuildShellAsync(connection, fullName, email, userRole, cancellationToken),
            ActiveTab = NormalizeTab(tab),
            TablesReady = await TableExistsAsync(connection, "PUAN_AYAR", cancellationToken)
                && await TableExistsAsync(connection, "PUAN_KULLANICI", cancellationToken)
        };

        if (!model.TablesReady)
        {
            return model;
        }

        model.KazanimRules = await LoadRulesAsync(connection, "KAZANIM", cancellationToken);
        model.IndirimRules = await LoadRulesAsync(connection, "INDIRIM", cancellationToken);

        if (editRuleId is > 0)
        {
            var editRow = model.KazanimRules.FirstOrDefault(x => x.Id == editRuleId)
                          ?? model.IndirimRules.FirstOrDefault(x => x.Id == editRuleId);
            if (editRow is not null)
            {
                model.RuleForm = new AdminPuanAyarForm
                {
                    Id = editRow.Id,
                    AyarTipi = editRow.AyarTipi,
                    MinDeger = editRow.MinDeger,
                    MaxDeger = editRow.MaxDeger,
                    PuanDegeri = editRow.PuanDegeri,
                    IndirimYuzde = editRow.IndirimYuzde,
                    Aciklama = editRow.Aciklama,
                    AktifMi = editRow.AktifMi,
                    SiraNo = editRow.SiraNo
                };
                model.ActiveTab = string.Equals(editRow.AyarTipi, "INDIRIM", StringComparison.OrdinalIgnoreCase) ? "indirim" : "kazanim";
            }
        }

        if (filterUserId is > 0)
        {
            model.AdjustForm.UserId = filterUserId.Value;
            model.UserBalances = await LoadUserBalancesAsync(connection, filterUserId.Value, cancellationToken);
            model.ActiveTab = "kullanici";
        }

        return model;
    }

    public async Task<(bool Success, string Message)> SaveRuleAsync(AdminPuanAyarForm form, CancellationToken cancellationToken = default)
    {
        if (form is null)
        {
            return (false, "Form verisi alınamadı.");
        }

        var ayarTipi = NormalizeAyarTipi(form.AyarTipi);
        if (form.MinDeger < 0)
        {
            return (false, "Minimum değer 0 veya daha büyük olmalıdır.");
        }

        if (form.MaxDeger.HasValue && form.MaxDeger.Value < form.MinDeger)
        {
            return (false, "Maksimum değer minimum değerden küçük olamaz.");
        }

        if (string.Equals(ayarTipi, "KAZANIM", StringComparison.OrdinalIgnoreCase))
        {
            if (!form.PuanDegeri.HasValue || form.PuanDegeri.Value <= 0)
            {
                return (false, "Kazanım kuralı için geçerli puan değeri girin.");
            }
        }
        else if (!form.IndirimYuzde.HasValue || form.IndirimYuzde.Value <= 0)
        {
            return (false, "İndirim kuralı için geçerli yüzde girin.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        if (!await TableExistsAsync(connection, "PUAN_AYAR", cancellationToken))
        {
            return (false, "PUAN_AYAR tablosu bulunamadı. Migration uygulayın.");
        }

        var siraNo = form.SiraNo <= 0 ? 100 : form.SiraNo;
        object aciklama = string.IsNullOrWhiteSpace(form.Aciklama) ? DBNull.Value : form.Aciklama.Trim();
        object maxDeger = form.MaxDeger.HasValue ? form.MaxDeger.Value : DBNull.Value;
        object puan = form.PuanDegeri.HasValue ? form.PuanDegeri.Value : DBNull.Value;
        object indirim = form.IndirimYuzde.HasValue ? form.IndirimYuzde.Value : DBNull.Value;

        if (form.Id is > 0)
        {
            const string updateSql = """
                UPDATE [dbo].[PUAN_AYAR]
                SET [AYAR_TIPI] = @tip,
                    [MIN_DEGER] = @minDeger,
                    [MAX_DEGER] = @maxDeger,
                    [PUAN_DEGERI] = @puan,
                    [INDIRIM_YUZDE] = @indirim,
                    [ACIKLAMA] = @aciklama,
                    [AKTIF_MI] = @aktif,
                    [SIRA_NO] = @sira,
                    [GUNCELLENME_TARIHI] = CURRENT_TIMESTAMP
                WHERE [ID] = @id;
                """;
            await using var command = new SqlCommand(updateSql, connection);
            BindRuleParameters(command, form.Id.Value, ayarTipi, form.MinDeger, maxDeger, puan, indirim, aciklama, form.AktifMi, siraNo);
            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
            {
                return (false, "Kayıt bulunamadı.");
            }
        }
        else
        {
            const string insertSql = """
                INSERT INTO [dbo].[PUAN_AYAR]
                    ([AYAR_TIPI], [MIN_DEGER], [MAX_DEGER], [PUAN_DEGERI], [INDIRIM_YUZDE], [ACIKLAMA], [AKTIF_MI], [SIRA_NO])
                VALUES
                    (@tip, @minDeger, @maxDeger, @puan, @indirim, @aciklama, @aktif, @sira);
                """;
            await using var command = new SqlCommand(insertSql, connection);
            BindRuleParameters(command, 0, ayarTipi, form.MinDeger, maxDeger, puan, indirim, aciklama, form.AktifMi, siraNo);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        _hotelPointsService.InvalidateCache();
        return (true, form.Id is > 0 ? "Kural güncellendi." : "Kural eklendi.");
    }

    public async Task<(bool Success, string Message)> DeleteRuleAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return (false, "Geçersiz kayıt.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("DELETE FROM [dbo].[PUAN_AYAR] WHERE [ID] = @id;", connection);
        command.Parameters.AddWithValue("@id", id);
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            return (false, "Kayıt bulunamadı.");
        }

        _hotelPointsService.InvalidateCache();
        return (true, "Kural silindi.");
    }

    public async Task<(bool Success, string Message)> AdjustUserPointsAsync(
        AdminPuanKullaniciAdjustForm form,
        CancellationToken cancellationToken = default)
    {
        if (form is null)
        {
            return (false, "Form verisi alınamadı.");
        }

        return await _hotelPointsService.AdjustUserHotelPointsAsync(
            form.UserId,
            form.HotelId,
            form.PointDelta,
            form.Reason,
            cancellationToken);
    }

    private static async Task<List<AdminPuanAyarRowViewModel>> LoadRulesAsync(
        SqlConnection connection,
        string ayarTipi,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                [ID], [AYAR_TIPI], [MIN_DEGER], [MAX_DEGER], [PUAN_DEGERI], [INDIRIM_YUZDE],
                COALESCE([ACIKLAMA], N''), [AKTIF_MI], [SIRA_NO],
                FORMAT([OLUSTURULMA_TARIHI], 'dd.MM.yyyy HH:mm', 'tr-TR')
            FROM [dbo].[PUAN_AYAR]
            WHERE [AYAR_TIPI] = @tip
            ORDER BY [SIRA_NO], [MIN_DEGER];
            """;

        var rows = new List<AdminPuanAyarRowViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tip", ayarTipi);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new AdminPuanAyarRowViewModel
            {
                Id = reader.GetInt64(0),
                AyarTipi = reader.GetString(1),
                MinDeger = reader.GetDecimal(2),
                MaxDeger = reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                PuanDegeri = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                IndirimYuzde = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                Aciklama = reader.GetString(6),
                AktifMi = reader.GetBoolean(7),
                SiraNo = reader.GetInt32(8),
                OlusturulmaTarihiText = reader.IsDBNull(9) ? "-" : reader.GetString(9)
            });
        }

        return rows;
    }

    private static async Task<List<AdminPuanKullaniciBalanceRowViewModel>> LoadUserBalancesAsync(
        SqlConnection connection,
        long userId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                pk.[KULLANICI_ID],
                COALESCE(u.[AD_SOYAD], N'') AS user_name,
                pk.[OTEL_ID],
                COALESCE(o.[OTEL_ADI], N'') AS hotel_name,
                pk.[KULLANILABILIR_PUAN],
                pk.[TOPLAM_KAZANILAN],
                pk.[KULLANILAN_PUAN]
            FROM [dbo].[PUAN_KULLANICI] pk
            INNER JOIN [dbo].[KULLANICILAR] u ON u.[ID] = pk.[KULLANICI_ID]
            INNER JOIN [dbo].[OTELLER] o ON o.[ID] = pk.[OTEL_ID]
            WHERE pk.[KULLANICI_ID] = @userId
            ORDER BY pk.[KULLANILABILIR_PUAN] DESC;
            """;

        var rows = new List<AdminPuanKullaniciBalanceRowViewModel>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new AdminPuanKullaniciBalanceRowViewModel
            {
                UserId = reader.GetInt64(0),
                UserDisplayName = reader.GetString(1),
                HotelId = reader.GetInt64(2),
                HotelName = reader.GetString(3),
                AvailablePoints = reader.GetInt32(4),
                TotalEarned = reader.GetInt32(5),
                UsedPoints = reader.GetInt32(6)
            });
        }

        return rows;
    }

    private static void BindRuleParameters(
        SqlCommand command,
        long id,
        string ayarTipi,
        decimal minDeger,
        object maxDeger,
        object puan,
        object indirim,
        object aciklama,
        bool aktif,
        int siraNo)
    {
        if (id > 0)
        {
            command.Parameters.AddWithValue("@id", id);
        }

        command.Parameters.AddWithValue("@tip", ayarTipi);
        command.Parameters.AddWithValue("@minDeger", minDeger);
        command.Parameters.AddWithValue("@maxDeger", maxDeger);
        command.Parameters.AddWithValue("@puan", puan);
        command.Parameters.AddWithValue("@indirim", indirim);
        command.Parameters.AddWithValue("@aciklama", aciklama);
        command.Parameters.AddWithValue("@aktif", aktif);
        command.Parameters.AddWithValue("@sira", siraNo);
    }

    private static string NormalizeTab(string? tab)
        => tab switch
        {
            "indirim" => "indirim",
            "kullanici" => "kullanici",
            _ => "kazanim"
        };

    private static string NormalizeAyarTipi(string? raw)
        => string.Equals(raw, "INDIRIM", StringComparison.OrdinalIgnoreCase) ? "INDIRIM" : "KAZANIM";

    private async Task<AdminShellViewModel> BuildShellAsync(
        SqlConnection connection,
        string fullName,
        string email,
        string userRole,
        CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT
                (SELECT COUNT(*) FROM [dbo].[PARTNER_DETAYLARI] WHERE [ONAY_DURUMU] = 'Beklemede') AS pending_partner_applications,
                (SELECT COUNT(*) FROM [dbo].[FIRMALAR] WHERE COALESCE([ONAY_DURUMU], 'Beklemede') = 'Beklemede') AS pending_company_applications,
                (SELECT COUNT(*) FROM [dbo].[SISTEM_ICI_BILDIRIMLER] WHERE [OKUNDU_MU] = 0) AS unread_notifications,
                (SELECT COUNT(*) FROM [dbo].[SISTEM_HATA_LOGLARI] WHERE [HATA_SEVIYESI] IN ('CRITICAL','ALERT','EMERGENCY') AND [COZULDU_MU] = 0) AS critical_logs,
                (SELECT COUNT(*) FROM [dbo].[YORUMLAR] WHERE [ONAY_DURUMU] = 'Beklemede') AS pending_reviews;";

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var shell = new AdminShellViewModel
        {
            FullName = fullName,
            Email = email,
            UserRole = userRole,
            PanelTitle = "Puan Yönetimi",
            PanelSubtitle = "Otel bazlı puan kazanım ve indirim kurallarını yönetin."
        };

        if (await reader.ReadAsync(cancellationToken))
        {
            shell.PendingPartnerApplications = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            shell.PendingCompanyApplications = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            shell.UnreadNotifications = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
            shell.CriticalLogs = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
            shell.PendingReviews = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
        }

        try
        {
            var rawUserId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(otelturizmnew.Constants.AuthClaimTypes.UserId)
                            ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (long.TryParse(rawUserId, out var adminUserId) && adminUserId > 0)
            {
                shell.Permissions = await _adminRbacService.GetPermissionsAsync(adminUserId, userRole, cancellationToken);
            }
        }
        catch
        {
            if (string.Equals(userRole, "superadmin", StringComparison.OrdinalIgnoreCase)
                || string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            {
                shell.Permissions.Add("*");
            }
        }

        return shell;
    }

    private static async Task<bool> TableExistsAsync(SqlConnection connection, string tableName, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(@"
            SELECT COUNT(*)
            FROM information_schema.TABLES
            WHERE TABLE_CATALOG = DB_NAME() AND TABLE_NAME = @tableName;", connection);
        command.Parameters.AddWithValue("@tableName", tableName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture) > 0;
    }
}
