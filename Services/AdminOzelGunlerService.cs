using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using otelturizmnew.Models.Paneller.Admin;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class AdminOzelGunlerService : IAdminOzelGunlerService
{
    private readonly string _connectionString;
    private readonly IAdminRbacService _adminRbacService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOzelGunService _ozelGunService;

    public AdminOzelGunlerService(
        IConfiguration configuration,
        IAdminRbacService adminRbacService,
        IHttpContextAccessor httpContextAccessor,
        IOzelGunService ozelGunService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _adminRbacService = adminRbacService;
        _httpContextAccessor = httpContextAccessor;
        _ozelGunService = ozelGunService;
    }

    public async Task<AdminOzelGunlerPageViewModel> GetPageAsync(string fullName, string email, string userRole, int? editId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var model = new AdminOzelGunlerPageViewModel
        {
            Shell = await BuildShellAsync(connection, fullName, email, userRole, cancellationToken)
        };

        if (!await TableExistsAsync(connection, cancellationToken))
        {
            return model;
        }

        const string sql = """
            SELECT
                [ID], [GUN_KODU], [GUN_ADI], [AY], [GUN], [KURAL_TIPI],
                [KURAL_PARAM1], [KURAL_PARAM2], [EMOJI], [KUTLAMA_METNI], [KATEGORI],
                [AKTIF_MI], [SIRALAMA],
                FORMAT([OLUSTURULMA_TARIHI], 'dd.MM.yyyy HH:mm', 'tr-TR')
            FROM [dbo].[OZEL_GUNLER]
            ORDER BY [SIRALAMA], [AY], [GUN_ADI];
            """;

        await using (var command = new SqlCommand(sql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                model.Rows.Add(MapRow(reader));
            }
        }

        model.TotalCount = model.Rows.Count;

        if (editId is > 0)
        {
            var editRow = model.Rows.FirstOrDefault(x => x.Id == editId.Value);
            if (editRow is not null)
            {
                model.Form = new AdminOzelGunForm
                {
                    Id = editRow.Id,
                    GunKodu = editRow.GunKodu,
                    GunAdi = editRow.GunAdi,
                    Ay = editRow.Ay,
                    Gun = editRow.Gun,
                    KuralTipi = editRow.KuralTipi,
                    KuralParam1 = editRow.KuralParam1,
                    KuralParam2 = editRow.KuralParam2,
                    Emoji = editRow.Emoji,
                    KutlamaMetni = editRow.KutlamaMetni,
                    Kategori = editRow.Kategori,
                    AktifMi = editRow.AktifMi,
                    Siralama = editRow.Siralama
                };
            }
        }

        return model;
    }

    public async Task<(bool Success, string Message)> SaveAsync(AdminOzelGunForm form, CancellationToken cancellationToken = default)
    {
        if (form is null)
        {
            return (false, "Form verisi alınamadı.");
        }

        var gunKodu = NormalizeGunKodu(form.GunKodu);
        var gunAdi = (form.GunAdi ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(gunKodu))
        {
            return (false, "Gün kodu zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(gunAdi))
        {
            return (false, "Gün adı zorunludur.");
        }

        if (form.Ay is < 1 or > 12)
        {
            return (false, "Ay 1–12 arasında olmalıdır.");
        }

        var kuralTipi = NormalizeKuralTipi(form.KuralTipi);
        byte? gun = form.Gun;
        byte? param1 = form.KuralParam1;
        byte? param2 = form.KuralParam2;

        if (string.Equals(kuralTipi, "SABIT", StringComparison.OrdinalIgnoreCase))
        {
            if (!gun.HasValue || gun.Value is < 1 or > 31)
            {
                return (false, "Sabit kural için geçerli bir gün (1–31) girin.");
            }

            param1 = null;
            param2 = null;
        }
        else
        {
            gun = null;
            if (!param1.HasValue || param1.Value is < 1 or > 5)
            {
                return (false, "Hafta kuralı için 1–5 arası hafta numarası girin.");
            }

            if (!param2.HasValue || param2.Value > 6)
            {
                return (false, "Hafta kuralı için haftanın gününü seçin (0=Pazar … 6=Cumartesi).");
            }
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        if (!await TableExistsAsync(connection, cancellationToken))
        {
            return (false, "OZEL_GUNLER tablosu bulunamadı. Migration uygulayın.");
        }

        if (await GunKoduExistsAsync(connection, gunKodu, form.Id, cancellationToken))
        {
            return (false, "Bu gün kodu zaten kullanılıyor.");
        }

        var siralama = form.Siralama <= 0 ? 100 : form.Siralama;
        object emoji = string.IsNullOrWhiteSpace(form.Emoji) ? DBNull.Value : form.Emoji.Trim();
        object kutlama = string.IsNullOrWhiteSpace(form.KutlamaMetni) ? DBNull.Value : form.KutlamaMetni.Trim();
        object kategori = string.IsNullOrWhiteSpace(form.Kategori) ? DBNull.Value : form.Kategori.Trim();

        if (form.Id is > 0)
        {
            const string updateSql = """
                UPDATE [dbo].[OZEL_GUNLER]
                SET [GUN_KODU] = @gunKodu,
                    [GUN_ADI] = @gunAdi,
                    [AY] = @ay,
                    [GUN] = @gun,
                    [KURAL_TIPI] = @kuralTipi,
                    [KURAL_PARAM1] = @param1,
                    [KURAL_PARAM2] = @param2,
                    [EMOJI] = @emoji,
                    [KUTLAMA_METNI] = @kutlama,
                    [KATEGORI] = @kategori,
                    [AKTIF_MI] = @aktif,
                    [SIRALAMA] = @siralama
                WHERE [ID] = @id;
                """;

            await using var command = new SqlCommand(updateSql, connection);
            BindSaveParameters(command, form.Id.Value, gunKodu, gunAdi, form.Ay, gun, kuralTipi, param1, param2, emoji, kutlama, kategori, form.AktifMi, siralama);
            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0)
            {
                return (false, "Kayıt bulunamadı.");
            }
        }
        else
        {
            const string insertSql = """
                INSERT INTO [dbo].[OZEL_GUNLER]
                    ([GUN_KODU], [GUN_ADI], [AY], [GUN], [KURAL_TIPI], [KURAL_PARAM1], [KURAL_PARAM2], [EMOJI], [KUTLAMA_METNI], [KATEGORI], [AKTIF_MI], [SIRALAMA])
                VALUES
                    (@gunKodu, @gunAdi, @ay, @gun, @kuralTipi, @param1, @param2, @emoji, @kutlama, @kategori, @aktif, @siralama);
                """;

            await using var command = new SqlCommand(insertSql, connection);
            BindSaveParameters(command, 0, gunKodu, gunAdi, form.Ay, gun, kuralTipi, param1, param2, emoji, kutlama, kategori, form.AktifMi, siralama);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        _ozelGunService.InvalidateCache();
        return (true, form.Id is > 0 ? "Özel gün güncellendi." : "Özel gün eklendi.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return (false, "Geçersiz kayıt.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = "DELETE FROM [dbo].[OZEL_GUNLER] WHERE [ID] = @id;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected == 0)
        {
            return (false, "Kayıt bulunamadı.");
        }

        _ozelGunService.InvalidateCache();
        return (true, "Özel gün silindi.");
    }

    private static AdminOzelGunRowViewModel MapRow(SqlDataReader reader)
    {
        return new AdminOzelGunRowViewModel
        {
            Id = reader.GetInt32(0),
            GunKodu = reader.GetString(1),
            GunAdi = reader.GetString(2),
            Ay = reader.GetByte(3),
            Gun = reader.IsDBNull(4) ? null : reader.GetByte(4),
            KuralTipi = reader.GetString(5),
            KuralParam1 = reader.IsDBNull(6) ? null : reader.GetByte(6),
            KuralParam2 = reader.IsDBNull(7) ? null : reader.GetByte(7),
            Emoji = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
            KutlamaMetni = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
            Kategori = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
            AktifMi = reader.GetBoolean(11),
            Siralama = reader.GetInt32(12),
            OlusturulmaTarihiText = reader.IsDBNull(13) ? "-" : reader.GetString(13)
        };
    }

    private static void BindSaveParameters(
        SqlCommand command,
        int id,
        string gunKodu,
        string gunAdi,
        byte ay,
        byte? gun,
        string kuralTipi,
        byte? param1,
        byte? param2,
        object emoji,
        object kutlama,
        object kategori,
        bool aktif,
        int siralama)
    {
        if (id > 0)
        {
            command.Parameters.AddWithValue("@id", id);
        }

        command.Parameters.AddWithValue("@gunKodu", gunKodu);
        command.Parameters.AddWithValue("@gunAdi", gunAdi);
        command.Parameters.AddWithValue("@ay", ay);
        command.Parameters.AddWithValue("@gun", gun.HasValue ? gun.Value : DBNull.Value);
        command.Parameters.AddWithValue("@kuralTipi", kuralTipi);
        command.Parameters.AddWithValue("@param1", param1.HasValue ? param1.Value : DBNull.Value);
        command.Parameters.AddWithValue("@param2", param2.HasValue ? param2.Value : DBNull.Value);
        command.Parameters.AddWithValue("@emoji", emoji);
        command.Parameters.AddWithValue("@kutlama", kutlama);
        command.Parameters.AddWithValue("@kategori", kategori);
        command.Parameters.AddWithValue("@aktif", aktif);
        command.Parameters.AddWithValue("@siralama", siralama);
    }

    private static async Task<bool> GunKoduExistsAsync(SqlConnection connection, string gunKodu, int? excludeId, CancellationToken cancellationToken)
    {
        var sql = excludeId is > 0
            ? "SELECT COUNT(*) FROM [dbo].[OZEL_GUNLER] WHERE [GUN_KODU] = @gunKodu AND [ID] <> @excludeId;"
            : "SELECT COUNT(*) FROM [dbo].[OZEL_GUNLER] WHERE [GUN_KODU] = @gunKodu;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@gunKodu", gunKodu);
        if (excludeId is > 0)
        {
            command.Parameters.AddWithValue("@excludeId", excludeId.Value);
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture) > 0;
    }

    private static string NormalizeGunKodu(string? raw)
    {
        var value = (raw ?? string.Empty).Trim().ToLowerInvariant();
        value = Regex.Replace(value, @"[^a-z0-9\-_]+", "-");
        value = Regex.Replace(value, @"-+", "-").Trim('-');
        return value;
    }

    private static string NormalizeKuralTipi(string? raw)
    {
        var value = (raw ?? string.Empty).Trim().ToUpperInvariant();
        return value == "NINCI_HAFTA_GUNU" ? "NINCI_HAFTA_GUNU" : "SABIT";
    }

    private async Task<AdminShellViewModel> BuildShellAsync(SqlConnection connection, string fullName, string email, string userRole, CancellationToken cancellationToken)
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
            PanelTitle = "Özel Günler",
            PanelSubtitle = "Anasayfa vitrininde gösterilen özel gün kayıtlarını yönetin."
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
            if (string.Equals(userRole, "superadmin", StringComparison.OrdinalIgnoreCase) || string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
            {
                shell.Permissions.Add("*");
            }
        }

        return shell;
    }

    private static async Task<bool> TableExistsAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM information_schema.TABLES
            WHERE TABLE_CATALOG = DB_NAME()
              AND TABLE_NAME = N'OZEL_GUNLER';
            """;
        await using var command = new SqlCommand(sql, connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture) > 0;
    }
}
