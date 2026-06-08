using System.Globalization;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using otelturizmnew.Models.Paneller.Admin;
using otelturizmnew.Services.Abstractions;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlTransaction = Microsoft.Data.SqlClient.SqlTransaction;

namespace otelturizmnew.Services;

public class AdminHomepageHotelsService : IAdminHomepageHotelsService
{
    private readonly string _connectionString;
    private readonly IAdminRbacService _adminRbacService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminHomepageHotelsService(
        IConfiguration configuration,
        IAdminRbacService adminRbacService,
        IHttpContextAccessor httpContextAccessor)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _adminRbacService = adminRbacService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<AdminHomepageHotelsPageViewModel> GetPageAsync(string fullName, string email, string userRole, long? activeSectionId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var shell = await BuildShellAsync(connection, "Anasayfa Aktif Oteller", "Anasayfa vitrin bölümlerini ve otel seçimlerini yönetin.", fullName, email, userRole, cancellationToken);
        var model = new AdminHomepageHotelsPageViewModel
        {
            Shell = shell,
            ActiveSectionId = activeSectionId
        };

        if (!await TableExistsAsync(connection, "ANASAYFA_OTEL_BOLUMLERI", cancellationToken))
        {
            return model;
        }

        const string sectionSql = @"
            SELECT [ID], [BOLUM_KODU], [BASLIK], [ALT_BASLIK], [SIRALAMA], [AKTIF_MI]
            FROM [dbo].[ANASAYFA_OTEL_BOLUMLERI]
            ORDER BY [SIRALAMA], [ID];";

        await using (var sectionCommand = new SqlCommand(sectionSql, connection))
        await using (var sectionReader = await sectionCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await sectionReader.ReadAsync(cancellationToken))
            {
                model.Sections.Add(new AdminHomepageHotelSectionViewModel
                {
                    Id = sectionReader.GetInt64(0),
                    SectionCode = sectionReader.GetString(1),
                    Title = sectionReader.GetString(2),
                    Subtitle = sectionReader.IsDBNull(3) ? null : sectionReader.GetString(3),
                    SortOrder = sectionReader.GetInt32(4),
                    IsActive = sectionReader.GetBoolean(5)
                });
            }
        }

        if (model.Sections.Count == 0)
        {
            return model;
        }

        if (!model.ActiveSectionId.HasValue || model.Sections.All(x => x.Id != model.ActiveSectionId.Value))
        {
            model.ActiveSectionId = model.Sections[0].Id;
        }

        const string hotelSql = @"
            SELECT k.[ID], k.[BOLUM_ID], k.[OTEL_ID], k.[SIRALAMA], k.[AKTIF_MI],
                   o.[OTEL_ADI], o.[SEHIR], o.[ILCE], COALESCE(o.[MAHALLE], N''), o.[YAYIN_DURUMU], o.[ONAY_DURUMU]
            FROM [dbo].[ANASAYFA_OTEL_KAYITLARI] k
            INNER JOIN [dbo].[OTELLER] o ON o.[ID] = k.[OTEL_ID]
            ORDER BY k.[BOLUM_ID], k.[SIRALAMA], k.[ID];";

        var hotelsBySection = new Dictionary<long, List<AdminHomepageHotelEntryViewModel>>();
        await using (var hotelCommand = new SqlCommand(hotelSql, connection))
        await using (var hotelReader = await hotelCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await hotelReader.ReadAsync(cancellationToken))
            {
                var sectionId = hotelReader.GetInt64(1);
                if (!hotelsBySection.TryGetValue(sectionId, out var list))
                {
                    list = new List<AdminHomepageHotelEntryViewModel>();
                    hotelsBySection[sectionId] = list;
                }

                list.Add(new AdminHomepageHotelEntryViewModel
                {
                    EntryId = hotelReader.GetInt64(0),
                    HotelId = hotelReader.GetInt64(2),
                    SortOrder = hotelReader.GetInt32(3),
                    IsActive = hotelReader.GetBoolean(4),
                    HotelName = hotelReader.GetString(5),
                    City = hotelReader.GetString(6),
                    District = hotelReader.GetString(7),
                    Neighborhood = hotelReader.GetString(8),
                    PublishStatus = hotelReader.GetString(9),
                    ApprovalStatus = hotelReader.GetString(10)
                });
            }
        }

        foreach (var section in model.Sections)
        {
            if (hotelsBySection.TryGetValue(section.Id, out var entries))
            {
                section.Hotels.AddRange(entries);
            }
        }

        var active = model.Sections.FirstOrDefault(x => x.Id == model.ActiveSectionId);
        if (active is not null)
        {
            model.SectionForm = new AdminHomepageSectionForm
            {
                Id = active.Id,
                Title = active.Title,
                Subtitle = active.Subtitle,
                SortOrder = active.SortOrder,
                IsActive = active.IsActive
            };
        }

        return model;
    }

    public async Task<IReadOnlyList<AdminHomepageHotelSearchResultViewModel>> SearchPublishedHotelsAsync(string? query, int limit = 20, CancellationToken cancellationToken = default)
    {
        var results = new List<AdminHomepageHotelSearchResultViewModel>();
        var search = (query ?? string.Empty).Trim();
        var safeLimit = Math.Clamp(limit, 1, 50);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT TOP (@limit)
                o.[ID],
                o.[OTEL_ADI],
                o.[SEHIR],
                o.[ILCE],
                COALESCE(o.[MAHALLE], N'')
            FROM [dbo].[OTELLER] o
            WHERE o.[YAYIN_DURUMU] = N'Yayında'
              AND o.[ONAY_DURUMU] IN (N'Onaylandı', N'Onaylandi', N'Onaylanmış', N'Onaylanmis', N'Onayli')
              AND (
                    @search = N''
                    OR o.[OTEL_ADI] LIKE N'%' + @search + N'%'
                    OR o.[OTEL_KODU] LIKE N'%' + @search + N'%'
                    OR o.[SEHIR] LIKE N'%' + @search + N'%'
                    OR o.[ILCE] LIKE N'%' + @search + N'%'
                    OR COALESCE(o.[MAHALLE], N'') LIKE N'%' + @search + N'%'
                  )
            ORDER BY o.[OTEL_ADI];";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@search", search);
        command.Parameters.AddWithValue("@limit", safeLimit);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var city = reader.GetString(2);
            var district = reader.GetString(3);
            var neighborhood = reader.GetString(4);
            results.Add(new AdminHomepageHotelSearchResultViewModel
            {
                HotelId = reader.GetInt64(0),
                HotelName = reader.GetString(1),
                City = city,
                District = district,
                Neighborhood = neighborhood,
                LocationLabel = BuildLocationLabel(neighborhood, district, city)
            });
        }

        return results;
    }

    public async Task<(bool Success, string Message)> AddHotelToSectionAsync(long sectionId, long hotelId, CancellationToken cancellationToken = default)
    {
        if (sectionId <= 0 || hotelId <= 0)
        {
            return (false, "Geçersiz bölüm veya otel.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        if (!await SectionExistsAsync(connection, sectionId, cancellationToken))
        {
            return (false, "Bölüm bulunamadı.");
        }

        if (!await PublishedHotelExistsAsync(connection, hotelId, cancellationToken))
        {
            return (false, "Otel yayında ve onaylı değil veya bulunamadı.");
        }

        if (await HotelAlreadyInSectionAsync(connection, sectionId, hotelId, cancellationToken))
        {
            return (false, "Bu otel zaten bu bölümde.");
        }

        var nextOrder = await GetNextHotelSortOrderAsync(connection, sectionId, cancellationToken);
        const string insertSql = @"
            INSERT INTO [dbo].[ANASAYFA_OTEL_KAYITLARI] ([BOLUM_ID], [OTEL_ID], [SIRALAMA], [AKTIF_MI])
            VALUES (@sectionId, @hotelId, @sortOrder, 1);";

        await using var command = new SqlCommand(insertSql, connection);
        command.Parameters.AddWithValue("@sectionId", sectionId);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        command.Parameters.AddWithValue("@sortOrder", nextOrder);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return (true, "Otel bölüme eklendi.");
    }

    public async Task<(bool Success, string Message)> RemoveHotelFromSectionAsync(long entryId, CancellationToken cancellationToken = default)
    {
        if (entryId <= 0)
        {
            return (false, "Geçersiz kayıt.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = "DELETE FROM [dbo].[ANASAYFA_OTEL_KAYITLARI] WHERE [ID] = @entryId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@entryId", entryId);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0 ? (true, "Otel bölümden kaldırıldı.") : (false, "Kayıt bulunamadı.");
    }

    public async Task<(bool Success, string Message)> ReorderHotelsAsync(long sectionId, IReadOnlyList<long> entryIds, CancellationToken cancellationToken = default)
    {
        if (sectionId <= 0 || entryIds.Count == 0)
        {
            return (false, "Sıralama için kayıt gerekli.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var order = 10;
            foreach (var entryId in entryIds)
            {
                const string sql = @"
                    UPDATE [dbo].[ANASAYFA_OTEL_KAYITLARI]
                    SET [SIRALAMA] = @sortOrder
                    WHERE [ID] = @entryId AND [BOLUM_ID] = @sectionId;";

                await using var command = new SqlCommand(sql, connection, transaction);
                command.Parameters.AddWithValue("@sortOrder", order);
                command.Parameters.AddWithValue("@entryId", entryId);
                command.Parameters.AddWithValue("@sectionId", sectionId);
                await command.ExecuteNonQueryAsync(cancellationToken);
                order += 10;
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, "Sıralama güncellendi.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<(bool Success, string Message)> MoveHotelAsync(long entryId, string direction, CancellationToken cancellationToken = default)
    {
        if (entryId <= 0)
        {
            return (false, "Geçersiz kayıt.");
        }

        var moveUp = string.Equals(direction, "up", StringComparison.OrdinalIgnoreCase);
        var moveDown = string.Equals(direction, "down", StringComparison.OrdinalIgnoreCase);
        if (!moveUp && !moveDown)
        {
            return (false, "Geçersiz yön.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        long sectionId;
        int currentOrder;
        const string currentSql = "SELECT [BOLUM_ID], [SIRALAMA] FROM [dbo].[ANASAYFA_OTEL_KAYITLARI] WHERE [ID] = @entryId;";
        await using (var currentCommand = new SqlCommand(currentSql, connection))
        {
            currentCommand.Parameters.AddWithValue("@entryId", entryId);
            await using var reader = await currentCommand.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return (false, "Kayıt bulunamadı.");
            }

            sectionId = reader.GetInt64(0);
            currentOrder = reader.GetInt32(1);
        }

        var neighborSql = moveUp
            ? @"SELECT TOP (1) [ID], [SIRALAMA] FROM [dbo].[ANASAYFA_OTEL_KAYITLARI]
                WHERE [BOLUM_ID] = @sectionId AND [SIRALAMA] < @sortOrder
                ORDER BY [SIRALAMA] DESC, [ID] DESC;"
            : @"SELECT TOP (1) [ID], [SIRALAMA] FROM [dbo].[ANASAYFA_OTEL_KAYITLARI]
                WHERE [BOLUM_ID] = @sectionId AND [SIRALAMA] > @sortOrder
                ORDER BY [SIRALAMA] ASC, [ID] ASC;";

        long? neighborId = null;
        int? neighborOrder = null;
        await using (var neighborCommand = new SqlCommand(neighborSql, connection))
        {
            neighborCommand.Parameters.AddWithValue("@sectionId", sectionId);
            neighborCommand.Parameters.AddWithValue("@sortOrder", currentOrder);
            await using var reader = await neighborCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                neighborId = reader.GetInt64(0);
                neighborOrder = reader.GetInt32(1);
            }
        }

        if (!neighborId.HasValue || !neighborOrder.HasValue)
        {
            return (false, moveUp ? "Otel zaten en üstte." : "Otel zaten en altta.");
        }

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using (var update1 = new SqlCommand("UPDATE [dbo].[ANASAYFA_OTEL_KAYITLARI] SET [SIRALAMA] = @sortOrder WHERE [ID] = @entryId;", connection, transaction))
            {
                update1.Parameters.AddWithValue("@sortOrder", neighborOrder.Value);
                update1.Parameters.AddWithValue("@entryId", entryId);
                await update1.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var update2 = new SqlCommand("UPDATE [dbo].[ANASAYFA_OTEL_KAYITLARI] SET [SIRALAMA] = @sortOrder WHERE [ID] = @entryId;", connection, transaction))
            {
                update2.Parameters.AddWithValue("@sortOrder", currentOrder);
                update2.Parameters.AddWithValue("@entryId", neighborId.Value);
                await update2.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, "Sıra güncellendi.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<(bool Success, string Message, long? SectionId)> CreateSectionAsync(AdminHomepageSectionForm form, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(form.Title))
        {
            return (false, "Bölüm başlığı zorunlu.", null);
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO [dbo].[ANASAYFA_OTEL_BOLUMLERI] ([BOLUM_KODU], [BASLIK], [ALT_BASLIK], [SIRALAMA], [AKTIF_MI])
            OUTPUT INSERTED.[ID]
            VALUES (N'custom', @title, @subtitle, @sortOrder, @active);";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@title", form.Title.Trim());
        command.Parameters.AddWithValue("@subtitle", (object?)form.Subtitle?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@sortOrder", form.SortOrder <= 0 ? 100 : form.SortOrder);
        command.Parameters.AddWithValue("@active", form.IsActive);
        var newId = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        return (true, "Yeni bölüm oluşturuldu.", newId);
    }

    public async Task<(bool Success, string Message)> UpdateSectionAsync(AdminHomepageSectionForm form, CancellationToken cancellationToken = default)
    {
        if (!form.Id.HasValue || form.Id.Value <= 0)
        {
            return (false, "Bölüm seçilmedi.");
        }

        if (string.IsNullOrWhiteSpace(form.Title))
        {
            return (false, "Bölüm başlığı zorunlu.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            UPDATE [dbo].[ANASAYFA_OTEL_BOLUMLERI]
            SET [BASLIK] = @title,
                [ALT_BASLIK] = @subtitle,
                [SIRALAMA] = @sortOrder,
                [AKTIF_MI] = @active,
                [GUNCELLENME_TARIHI] = sysutcdatetime()
            WHERE [ID] = @sectionId;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@sectionId", form.Id.Value);
        command.Parameters.AddWithValue("@title", form.Title.Trim());
        command.Parameters.AddWithValue("@subtitle", (object?)form.Subtitle?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@sortOrder", form.SortOrder <= 0 ? 100 : form.SortOrder);
        command.Parameters.AddWithValue("@active", form.IsActive);
        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0 ? (true, "Bölüm güncellendi.") : (false, "Bölüm bulunamadı.");
    }

    public async Task<(bool Success, string Message)> DeleteSectionAsync(long sectionId, CancellationToken cancellationToken = default)
    {
        if (sectionId <= 0)
        {
            return (false, "Geçersiz bölüm.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string codeSql = "SELECT [BOLUM_KODU] FROM [dbo].[ANASAYFA_OTEL_BOLUMLERI] WHERE [ID] = @sectionId;";
        string? sectionCode;
        await using (var codeCommand = new SqlCommand(codeSql, connection))
        {
            codeCommand.Parameters.AddWithValue("@sectionId", sectionId);
            var scalar = await codeCommand.ExecuteScalarAsync(cancellationToken);
            if (scalar is null or DBNull)
            {
                return (false, "Bölüm bulunamadı.");
            }

            sectionCode = Convert.ToString(scalar, CultureInfo.InvariantCulture);
        }

        if (string.Equals(sectionCode, "ozel-rotalar", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Seçilen Özel Rotalar bölümü silinemez.");
        }

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using (var deleteHotels = new SqlCommand("DELETE FROM [dbo].[ANASAYFA_OTEL_KAYITLARI] WHERE [BOLUM_ID] = @sectionId;", connection, transaction))
            {
                deleteHotels.Parameters.AddWithValue("@sectionId", sectionId);
                await deleteHotels.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var deleteSection = new SqlCommand("DELETE FROM [dbo].[ANASAYFA_OTEL_BOLUMLERI] WHERE [ID] = @sectionId;", connection, transaction))
            {
                deleteSection.Parameters.AddWithValue("@sectionId", sectionId);
                var affected = await deleteSection.ExecuteNonQueryAsync(cancellationToken);
                if (affected == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return (false, "Bölüm bulunamadı.");
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, "Bölüm silindi.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
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

    private static async Task<bool> TableExistsAsync(SqlConnection connection, string tableName, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM information_schema.TABLES
            WHERE TABLE_CATALOG = DB_NAME()
              AND TABLE_NAME = @tableName;
            """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tableName", tableName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture) > 0;
    }

    private static async Task<bool> SectionExistsAsync(SqlConnection connection, long sectionId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(*) FROM [dbo].[ANASAYFA_OTEL_BOLUMLERI] WHERE [ID] = @sectionId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@sectionId", sectionId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture) > 0;
    }

    private static async Task<bool> PublishedHotelExistsAsync(SqlConnection connection, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM [dbo].[OTELLER]
            WHERE [ID] = @hotelId
              AND [YAYIN_DURUMU] = N'Yayında'
              AND [ONAY_DURUMU] IN (N'Onaylandı', N'Onaylandi', N'Onaylanmış', N'Onaylanmis', N'Onayli');";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture) > 0;
    }

    private static async Task<bool> HotelAlreadyInSectionAsync(SqlConnection connection, long sectionId, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(*) FROM [dbo].[ANASAYFA_OTEL_KAYITLARI] WHERE [BOLUM_ID] = @sectionId AND [OTEL_ID] = @hotelId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@sectionId", sectionId);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture) > 0;
    }

    private static async Task<int> GetNextHotelSortOrderAsync(SqlConnection connection, long sectionId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COALESCE(MAX([SIRALAMA]), 0) + 10 FROM [dbo].[ANASAYFA_OTEL_KAYITLARI] WHERE [BOLUM_ID] = @sectionId;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@sectionId", sectionId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    private static string BuildLocationLabel(string neighborhood, string district, string city)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(neighborhood))
        {
            parts.Add(neighborhood.Trim());
        }

        if (!string.IsNullOrWhiteSpace(district))
        {
            parts.Add(district.Trim());
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            parts.Add(city.Trim());
        }

        return parts.Count > 0 ? string.Join(", ", parts) : "-";
    }

    private static int SafeInt(SqlDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return 0;
        }

        return Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }
}
