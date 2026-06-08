using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using otelturizmnew.Models.Paneller.Admin;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class AdminLocationService : IAdminLocationService
{
    private readonly string _connectionString;
    private readonly IAdminRbacService _adminRbacService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminLocationService(IConfiguration configuration, IAdminRbacService adminRbacService, IHttpContextAccessor httpContextAccessor)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _adminRbacService = adminRbacService;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<AdminLocationListPageViewModel> GetCitiesPageAsync(string fullName, string email, string userRole, string? searchTerm = null, string? activeFilter = null, long? countryId = null, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default)
        => GetPageAsync(AdminLocationEntityType.City, fullName, email, userRole, searchTerm, activeFilter, countryId, null, null, page, pageSize, cancellationToken);

    public Task<AdminLocationListPageViewModel> GetDistrictsPageAsync(string fullName, string email, string userRole, string? searchTerm = null, string? activeFilter = null, long? countryId = null, long? cityId = null, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default)
        => GetPageAsync(AdminLocationEntityType.District, fullName, email, userRole, searchTerm, activeFilter, countryId, cityId, null, page, pageSize, cancellationToken);

    public Task<AdminLocationListPageViewModel> GetNeighborhoodsPageAsync(string fullName, string email, string userRole, string? searchTerm = null, string? activeFilter = null, long? countryId = null, long? cityId = null, long? districtId = null, int page = 1, int pageSize = 25, CancellationToken cancellationToken = default)
        => GetPageAsync(AdminLocationEntityType.Neighborhood, fullName, email, userRole, searchTerm, activeFilter, countryId, cityId, districtId, page, pageSize, cancellationToken);

    private async Task<AdminLocationListPageViewModel> GetPageAsync(
        AdminLocationEntityType entityType,
        string fullName,
        string email,
        string userRole,
        string? searchTerm,
        string? activeFilter,
        long? countryId,
        long? cityId,
        long? districtId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var (title, subtitle) = entityType switch
        {
            AdminLocationEntityType.City => ("İller", "Türkiye ve uluslararası il/eyalet kayıtlarını arayın, filtreleyin ve durumlarını izleyin."),
            AdminLocationEntityType.District => ("İlçeler", "İl bazında ilçe kayıtlarını arayın ve aktif/pasif durumlarını yönetmeye hazırlayın."),
            _ => ("Mahalleler", "İlçe bazında mahalle kayıtlarını arayın; SEO slug ve posta kodu bilgilerini görüntüleyin.")
        };

        var safePageSize = Math.Clamp(pageSize <= 0 ? 25 : pageSize, 10, 100);
        var model = new AdminLocationListPageViewModel
        {
            EntityType = entityType,
            Shell = await BuildShellAsync(connection, title, subtitle, fullName, email, userRole, cancellationToken),
            SearchTerm = searchTerm?.Trim() ?? string.Empty,
            ActiveFilter = activeFilter?.Trim() ?? string.Empty,
            CountryIdFilter = countryId > 0 ? countryId : null,
            CityIdFilter = cityId > 0 ? cityId : null,
            DistrictIdFilter = districtId > 0 ? districtId : null,
            Page = Math.Max(1, page),
            PageSize = safePageSize
        };

        model.SummaryCards.AddRange(await LoadSummaryCardsAsync(connection, entityType, cancellationToken));
        await LoadFilterOptionsAsync(connection, model, cancellationToken);

        var (fromSql, whereSql, selectSql) = BuildQueries(entityType);
        var countSql = $"SELECT COUNT(*) {fromSql} {whereSql};";
        await using (var countCmd = new SqlCommand(countSql, connection))
        {
            BindFilters(countCmd, model);
            model.TotalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
        }

        if (model.TotalCount > 0 && model.Page > model.TotalPages)
        {
            model.Page = model.TotalPages;
        }

        var listSql = $@"
            {selectSql}
            {fromSql}
            {whereSql}
            ORDER BY primary_label ASC, id ASC
            OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;";

        await using var cmd = new SqlCommand(listSql, connection);
        BindFilters(cmd, model);
        cmd.Parameters.AddWithValue("@offset", (model.Page - 1) * model.PageSize);
        cmd.Parameters.AddWithValue("@pageSize", model.PageSize);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            model.Rows.Add(new AdminLocationRowViewModel
            {
                Id = reader.GetInt64(0),
                PrimaryLabel = reader.IsDBNull(1) ? "-" : reader.GetString(1),
                SecondaryLabel = reader.IsDBNull(2) ? "-" : reader.GetString(2),
                TertiaryLabel = reader.IsDBNull(3) ? "-" : reader.GetString(3),
                Slug = reader.IsDBNull(4) ? "-" : reader.GetString(4),
                StatusLabel = reader.IsDBNull(5) ? "-" : reader.GetString(5),
                RegionLabel = reader.IsDBNull(6) ? "-" : reader.GetString(6),
                UpdatedAtText = reader.IsDBNull(7) ? "-" : reader.GetString(7)
            });
        }

        return model;
    }

    private static (string FromSql, string WhereSql, string SelectSql) BuildQueries(AdminLocationEntityType entityType)
    {
        return entityType switch
        {
            AdminLocationEntityType.City => (
                "FROM [dbo].[ILLER] i LEFT JOIN [dbo].[ULKELER] u ON u.[ID] = i.[ULKE_ID]",
                @"WHERE (@search = '' OR i.[IL_ADI] LIKE '%' + @search + '%' OR i.[SEO_SLUG] LIKE '%' + @search + '%' OR CAST(i.[PLAKA_KODU] AS nvarchar(10)) LIKE '%' + @search + '%')
                      AND (@countryId IS NULL OR i.[ULKE_ID] = @countryId)
                      AND (@activeFilter = '' OR (@activeFilter = 'active' AND i.[AKTIF_MI] = 1) OR (@activeFilter = 'inactive' AND i.[AKTIF_MI] = 0))",
                @"SELECT i.[ID] AS id,
                         i.[IL_ADI] AS primary_label,
                         COALESCE(u.[ULKE_ADI], N'-') AS secondary_label,
                         COALESCE(i.[BOLGE], N'-') AS tertiary_label,
                         i.[SEO_SLUG] AS slug,
                         CASE WHEN i.[AKTIF_MI] = 1 THEN N'Aktif' ELSE N'Pasif' END AS status_label,
                         COALESCE(i.[BOLGE_TIPI], N'IL') AS region_label,
                         COALESCE(FORMAT(i.[GUNCELLENME_TARIHI], 'dd.MM.yyyy HH:mm', 'tr-TR'), FORMAT(i.[OLUSTURULMA_TARIHI], 'dd.MM.yyyy', 'tr-TR'), N'-') AS updated_at"),
            AdminLocationEntityType.District => (
                "FROM [dbo].[ILCELER] c INNER JOIN [dbo].[ILLER] i ON i.[ID] = c.[IL_ID] LEFT JOIN [dbo].[ULKELER] u ON u.[ID] = c.[ULKE_ID]",
                @"WHERE (@search = '' OR c.[ILCE_ADI] LIKE '%' + @search + '%' OR c.[SEO_SLUG] LIKE '%' + @search + '%' OR i.[IL_ADI] LIKE '%' + @search + '%')
                      AND (@countryId IS NULL OR c.[ULKE_ID] = @countryId)
                      AND (@cityId IS NULL OR c.[IL_ID] = @cityId)
                      AND (@activeFilter = '' OR (@activeFilter = 'active' AND c.[AKTIF_MI] = 1) OR (@activeFilter = 'inactive' AND c.[AKTIF_MI] = 0))",
                @"SELECT c.[ID] AS id,
                         c.[ILCE_ADI] AS primary_label,
                         i.[IL_ADI] AS secondary_label,
                         COALESCE(u.[ULKE_ADI], N'-') AS tertiary_label,
                         c.[SEO_SLUG] AS slug,
                         CASE WHEN c.[AKTIF_MI] = 1 THEN N'Aktif' ELSE N'Pasif' END AS status_label,
                         CASE WHEN c.[MERKEZ_MI] = 1 THEN N'Merkez' ELSE N'İlçe' END AS region_label,
                         COALESCE(FORMAT(c.[GUNCELLENME_TARIHI], 'dd.MM.yyyy HH:mm', 'tr-TR'), FORMAT(c.[OLUSTURULMA_TARIHI], 'dd.MM.yyyy', 'tr-TR'), N'-') AS updated_at"),
            _ => (
                "FROM [dbo].[MAHALLELER] m INNER JOIN [dbo].[ILCELER] c ON c.[ID] = m.[ILCE_ID] INNER JOIN [dbo].[ILLER] i ON i.[ID] = m.[IL_ID] LEFT JOIN [dbo].[ULKELER] u ON u.[ID] = m.[ULKE_ID]",
                @"WHERE (@search = '' OR m.[MAHALLE_ADI] LIKE '%' + @search + '%' OR m.[SEO_SLUG] LIKE '%' + @search + '%' OR c.[ILCE_ADI] LIKE '%' + @search + '%' OR i.[IL_ADI] LIKE '%' + @search + '%' OR COALESCE(m.[POSTA_KODU], '') LIKE '%' + @search + '%')
                      AND (@countryId IS NULL OR m.[ULKE_ID] = @countryId)
                      AND (@cityId IS NULL OR m.[IL_ID] = @cityId)
                      AND (@districtId IS NULL OR m.[ILCE_ID] = @districtId)
                      AND (@activeFilter = '' OR (@activeFilter = 'active' AND m.[AKTIF_MI] = 1) OR (@activeFilter = 'inactive' AND m.[AKTIF_MI] = 0))",
                @"SELECT m.[ID] AS id,
                         m.[MAHALLE_ADI] AS primary_label,
                         c.[ILCE_ADI] AS secondary_label,
                         i.[IL_ADI] AS tertiary_label,
                         m.[SEO_SLUG] AS slug,
                         CASE WHEN m.[AKTIF_MI] = 1 THEN N'Aktif' ELSE N'Pasif' END AS status_label,
                         COALESCE(m.[POSTA_KODU], N'-') AS region_label,
                         COALESCE(FORMAT(m.[GUNCELLENME_TARIHI], 'dd.MM.yyyy HH:mm', 'tr-TR'), FORMAT(m.[OLUSTURULMA_TARIHI], 'dd.MM.yyyy', 'tr-TR'), N'-') AS updated_at")
        };
    }

    private static void BindFilters(SqlCommand command, AdminLocationListPageViewModel model)
    {
        command.Parameters.AddWithValue("@search", model.SearchTerm);
        command.Parameters.AddWithValue("@activeFilter", model.ActiveFilter);
        command.Parameters.AddWithValue("@countryId", (object?)model.CountryIdFilter ?? DBNull.Value);
        command.Parameters.AddWithValue("@cityId", (object?)model.CityIdFilter ?? DBNull.Value);
        command.Parameters.AddWithValue("@districtId", (object?)model.DistrictIdFilter ?? DBNull.Value);
    }

    private static async Task<List<AdminSummaryCardViewModel>> LoadSummaryCardsAsync(SqlConnection connection, AdminLocationEntityType entityType, CancellationToken cancellationToken)
    {
        var sql = entityType switch
        {
            AdminLocationEntityType.City => @"
                SELECT
                    (SELECT COUNT(*) FROM [dbo].[ILLER]) AS total_count,
                    (SELECT COUNT(*) FROM [dbo].[ILLER] WHERE [AKTIF_MI] = 1) AS active_count,
                    (SELECT COUNT(*) FROM [dbo].[ULKELER] WHERE [AKTIF_MI] = 1) AS country_count,
                    (SELECT COUNT(*) FROM [dbo].[ILLER] WHERE [BOLGE_TIPI] <> N'IL') AS intl_count",
            AdminLocationEntityType.District => @"
                SELECT
                    (SELECT COUNT(*) FROM [dbo].[ILCELER]) AS total_count,
                    (SELECT COUNT(*) FROM [dbo].[ILCELER] WHERE [AKTIF_MI] = 1) AS active_count,
                    (SELECT COUNT(DISTINCT [IL_ID]) FROM [dbo].[ILCELER]) AS city_count,
                    (SELECT COUNT(*) FROM [dbo].[ILCELER] WHERE [MERKEZ_MI] = 1) AS center_count",
            _ => @"
                SELECT
                    (SELECT COUNT(*) FROM [dbo].[MAHALLELER]) AS total_count,
                    (SELECT COUNT(*) FROM [dbo].[MAHALLELER] WHERE [AKTIF_MI] = 1) AS active_count,
                    (SELECT COUNT(DISTINCT [ILCE_ID]) FROM [dbo].[MAHALLELER]) AS district_count,
                    (SELECT COUNT(*) FROM [dbo].[MAHALLELER] WHERE [POSTA_KODU] IS NOT NULL AND LTRIM(RTRIM([POSTA_KODU])) <> '') AS postal_count"
        };

        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return [];
        }

        return entityType switch
        {
            AdminLocationEntityType.City =>
            [
                Card("Toplam İl", reader, 0, "Tüm kayıtlar", "info", "fa-map"),
                Card("Aktif İl", reader, 1, "Yayında konumlar", "success", "fa-circle-check"),
                Card("Ülke", reader, 2, "Bağlı ülke sayısı", "warning", "fa-globe"),
                Card("Uluslararası", reader, 3, "IL dışı bölge tipi", "danger", "fa-earth-americas")
            ],
            AdminLocationEntityType.District =>
            [
                Card("Toplam İlçe", reader, 0, "Tüm kayıtlar", "info", "fa-map-location-dot"),
                Card("Aktif İlçe", reader, 1, "Kullanımda", "success", "fa-circle-check"),
                Card("İl Kapsamı", reader, 2, "İlçeli il sayısı", "warning", "fa-city"),
                Card("Merkez İlçe", reader, 3, "Merkez işaretli", "danger", "fa-location-crosshairs")
            ],
            _ =>
            [
                Card("Toplam Mahalle", reader, 0, "Tüm kayıtlar", "info", "fa-house"),
                Card("Aktif Mahalle", reader, 1, "Kullanımda", "success", "fa-circle-check"),
                Card("İlçe Kapsamı", reader, 2, "Mahalleli ilçe", "warning", "fa-map-pin"),
                Card("Posta Kodlu", reader, 3, "Posta kodu dolu", "danger", "fa-envelope")
            ]
        };
    }

    private static AdminSummaryCardViewModel Card(string label, SqlDataReader reader, int index, string description, string tone, string icon)
        => new()
        {
            Label = label,
            Value = reader.IsDBNull(index) ? "0" : reader.GetValue(index)?.ToString() ?? "0",
            Description = description,
            ToneClass = tone,
            IconClass = icon
        };

    private static async Task LoadFilterOptionsAsync(SqlConnection connection, AdminLocationListPageViewModel model, CancellationToken cancellationToken)
    {
        const string countriesSql = "SELECT [ID], [ULKE_ADI] FROM [dbo].[ULKELER] WHERE [AKTIF_MI] = 1 ORDER BY [ULKE_ADI];";
        await using (var cmd = new SqlCommand(countriesSql, connection))
        await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                model.CountryOptions.Add(new AdminLocationOptionViewModel
                {
                    Id = reader.GetInt64(0),
                    Label = reader.GetString(1)
                });
            }
        }

        if (model.EntityType is AdminLocationEntityType.District or AdminLocationEntityType.Neighborhood)
        {
            var citiesSql = model.CountryIdFilter.HasValue
                ? "SELECT [ID], [IL_ADI] FROM [dbo].[ILLER] WHERE [AKTIF_MI] = 1 AND [ULKE_ID] = @countryId ORDER BY [IL_ADI];"
                : "SELECT [ID], [IL_ADI] FROM [dbo].[ILLER] WHERE [AKTIF_MI] = 1 ORDER BY [IL_ADI];";
            await using var cmd = new SqlCommand(citiesSql, connection);
            if (model.CountryIdFilter.HasValue)
            {
                cmd.Parameters.AddWithValue("@countryId", model.CountryIdFilter.Value);
            }

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                model.CityOptions.Add(new AdminLocationOptionViewModel
                {
                    Id = reader.GetInt64(0),
                    Label = reader.GetString(1)
                });
            }
        }

        if (model.EntityType == AdminLocationEntityType.Neighborhood)
        {
            var districtsSql = model.CityIdFilter.HasValue
                ? "SELECT [ID], [ILCE_ADI] FROM [dbo].[ILCELER] WHERE [AKTIF_MI] = 1 AND [IL_ID] = @cityId ORDER BY [ILCE_ADI];"
                : "SELECT TOP (300) [ID], [ILCE_ADI] FROM [dbo].[ILCELER] WHERE [AKTIF_MI] = 1 ORDER BY [ILCE_ADI];";
            await using var cmd = new SqlCommand(districtsSql, connection);
            if (model.CityIdFilter.HasValue)
            {
                cmd.Parameters.AddWithValue("@cityId", model.CityIdFilter.Value);
            }

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                model.DistrictOptions.Add(new AdminLocationOptionViewModel
                {
                    Id = reader.GetInt64(0),
                    Label = reader.GetString(1)
                });
            }
        }
    }

    private async Task<AdminShellViewModel> BuildShellAsync(SqlConnection connection, string title, string subtitle, string fullName, string email, string userRole, CancellationToken cancellationToken)
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
            PanelTitle = title,
            PanelSubtitle = subtitle
        };

        if (await reader.ReadAsync(cancellationToken))
        {
            shell.PendingPartnerApplications = SafeInt(reader, 0);
            shell.PendingCompanyApplications = SafeInt(reader, 1);
            shell.UnreadNotifications = SafeInt(reader, 2);
            shell.CriticalLogs = SafeInt(reader, 3);
            shell.PendingReviews = SafeInt(reader, 4);
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

    private static int SafeInt(SqlDataReader reader, int index)
        => reader.IsDBNull(index) ? 0 : Convert.ToInt32(reader.GetValue(index), CultureInfo.InvariantCulture);
}
