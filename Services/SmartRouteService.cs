using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using otelturizmnew.Models.Oteller;
using otelturizmnew.Models.Paneller.Partner;
using otelturizmnew.Services.Abstractions;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;

namespace otelturizmnew.Services;

public sealed class SmartRouteService : ISmartRouteService
{
    private const string ListingFiltersCacheKey = "akilli-rota:listing-filters:v1";
    private static readonly TimeSpan ListingFiltersCacheDuration = TimeSpan.FromMinutes(10);

    private readonly string _connectionString;
    private readonly IPartnerService _partnerService;
    private readonly IMemoryCache _cache;

    public SmartRouteService(IConfiguration configuration, IPartnerService partnerService, IMemoryCache memoryCache)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _partnerService = partnerService;
        _cache = memoryCache;
    }

    public void InvalidateCache()
    {
        _cache.Remove(ListingFiltersCacheKey);
    }

    public async Task<List<SmartRouteFilterViewModel>> GetListingFiltersAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(ListingFiltersCacheKey, out List<SmartRouteFilterViewModel>? cached) && cached is not null)
        {
            return cached;
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "AKILLI_ROTA", cancellationToken))
        {
            return new List<SmartRouteFilterViewModel>();
        }

        const string sql = """
            SELECT
                ar.[ID],
                ar.[ETIKET_KODU],
                ar.[ETIKET_ADI],
                ar.[HASHTAG],
                COALESCE(ar.[ARAMA_METNI], '') AS [ARAMA_METNI],
                COALESCE(NULLIF(LTRIM(RTRIM(ar.[RENK_SINIFI])), ''), N'sage') AS [RENK_SINIFI],
                (
                    SELECT COUNT(DISTINCT aro.[OTEL_ID])
                    FROM [dbo].[AKILLI_ROTA_OTELLER] aro
                    INNER JOIN [dbo].[OTELLER] o ON o.[ID] = aro.[OTEL_ID]
                    WHERE aro.[AKILLI_ROTA_ID] = ar.[ID]
                      AND aro.[AKTIF_MI] = 1
                      AND (aro.[BITIS_TARIHI] IS NULL OR aro.[BITIS_TARIHI] > sysutcdatetime())
                      AND LOWER(REPLACE(LTRIM(RTRIM(o.[YAYIN_DURUMU])), NCHAR(0x0131), N'i')) = N'yayinda'
                ) AS [OTEL_ADEDI]
            FROM [dbo].[AKILLI_ROTA] ar
            WHERE ar.[AKTIF_MI] = 1
            ORDER BY ar.[SIRA_NO], ar.[ETIKET_ADI];
            """;

        var tags = new List<SmartRouteFilterViewModel>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tags.Add(new SmartRouteFilterViewModel
            {
                Id = reader.GetInt64(0),
                Slug = reader.GetString(1).Trim().ToLowerInvariant(),
                DisplayName = reader.GetString(2),
                Hashtag = reader.GetString(3),
                SearchText = reader.GetString(4),
                ColorClass = NormalizeColorClass(reader.GetString(5)),
                HotelCount = reader.IsDBNull(6) ? 0 : Convert.ToInt32(reader.GetValue(6))
            });
        }

        _cache.Set(ListingFiltersCacheKey, tags, ListingFiltersCacheDuration);
        return tags;
    }

    public async Task EnrichListingCardsAsync(IReadOnlyList<HotelListingCardViewModel> cards, CancellationToken cancellationToken = default)
    {
        if (cards.Count == 0)
        {
            return;
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "AKILLI_ROTA_OTELLER", cancellationToken))
        {
            return;
        }

        var hotelIds = cards.Select(c => c.Id).Distinct().ToList();
        var slugMap = await LoadHotelSlugMapAsync(connection, hotelIds, cancellationToken);
        foreach (var card in cards)
        {
            card.SmartRouteSlugs = slugMap.TryGetValue(card.Id, out var slugs)
                ? slugs
                : new List<string>();
        }
    }

    public async Task<PartnerSmartRoutesPageViewModel> GetPartnerPageAsync(long userId, long? hotelId, CancellationToken cancellationToken = default)
    {
        var dash = await _partnerService.GetDashboardAsync(userId, hotelId, cancellationToken: cancellationToken);
        var model = new PartnerSmartRoutesPageViewModel
        {
            Shell = dash.Shell,
            HotelId = dash.Shell.SelectedHotelId ?? 0,
            HotelName = dash.Shell.SelectedHotelName ?? "—"
        };

        if (model.HotelId <= 0)
        {
            return model;
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "AKILLI_ROTA", cancellationToken))
        {
            model.TablesReady = false;
            return model;
        }

        const string sql = """
            SELECT
                ar.[ID],
                ar.[ETIKET_KODU],
                ar.[ETIKET_ADI],
                ar.[HASHTAG],
                COALESCE(ar.[ARAMA_METNI], '') AS [ARAMA_METNI],
                COALESCE(NULLIF(LTRIM(RTRIM(ar.[RENK_SINIFI])), ''), N'sage') AS [RENK_SINIFI],
                aro.[ID] AS [UYELIK_ID],
                aro.[KATILMA_TARIHI],
                aro.[BITIS_TARIHI],
                CASE
                    WHEN aro.[ID] IS NOT NULL
                         AND aro.[AKTIF_MI] = 1
                         AND (aro.[BITIS_TARIHI] IS NULL OR aro.[BITIS_TARIHI] > sysutcdatetime())
                    THEN 1 ELSE 0
                END AS [KATILDI_MI]
            FROM [dbo].[AKILLI_ROTA] ar
            LEFT JOIN [dbo].[AKILLI_ROTA_OTELLER] aro
                ON aro.[AKILLI_ROTA_ID] = ar.[ID]
               AND aro.[OTEL_ID] = @hotelId
            WHERE ar.[AKTIF_MI] = 1
            ORDER BY ar.[SIRA_NO], ar.[ETIKET_ADI];
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@hotelId", model.HotelId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            model.Tags.Add(new PartnerSmartRouteTagViewModel
            {
                Id = reader.GetInt64(0),
                Slug = reader.GetString(1).Trim().ToLowerInvariant(),
                DisplayName = reader.GetString(2),
                Hashtag = reader.GetString(3),
                SearchText = reader.GetString(4),
                ColorClass = NormalizeColorClass(reader.GetString(5)),
                IsJoined = !reader.IsDBNull(9) && Convert.ToInt32(reader.GetValue(9)) == 1,
                JoinDate = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                EndDate = reader.IsDBNull(8) ? null : reader.GetDateTime(8)
            });
        }

        return model;
    }

    public async Task<(bool Success, string Message)> ToggleMembershipAsync(long userId, PartnerSmartRouteToggleRequest request, CancellationToken cancellationToken = default)
    {
        if (request.HotelId <= 0 || request.SmartRouteId <= 0)
        {
            return (false, "Geçersiz otel veya etiket seçimi.");
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        if (!await TableExistsAsync(connection, "AKILLI_ROTA_OTELLER", cancellationToken))
        {
            return (false, "Akıllı Rota tabloları henüz kurulmamış.");
        }

        if (!await HasHotelAccessAsync(connection, userId, request.HotelId, cancellationToken))
        {
            return (false, "Bu otel için yetkiniz bulunmuyor.");
        }

        if (request.Join)
        {
            const string upsertSql = """
                IF EXISTS (
                    SELECT 1 FROM [dbo].[AKILLI_ROTA_OTELLER]
                    WHERE [AKILLI_ROTA_ID] = @routeId AND [OTEL_ID] = @hotelId
                )
                BEGIN
                    UPDATE [dbo].[AKILLI_ROTA_OTELLER]
                    SET [AKTIF_MI] = 1,
                        [BITIS_TARIHI] = NULL,
                        [KATILMA_TARIHI] = sysutcdatetime(),
                        [GUNCELLENME_TARIHI] = sysutcdatetime(),
                        [KULLANICI_ID] = @userId
                    WHERE [AKILLI_ROTA_ID] = @routeId AND [OTEL_ID] = @hotelId;
                END
                ELSE
                BEGIN
                    INSERT INTO [dbo].[AKILLI_ROTA_OTELLER]
                        ([AKILLI_ROTA_ID], [OTEL_ID], [KATILMA_TARIHI], [AKTIF_MI], [KULLANICI_ID])
                    VALUES
                        (@routeId, @hotelId, sysutcdatetime(), 1, @userId);
                END
                """;

            await using var command = new SqlCommand(upsertSql, connection);
            command.Parameters.AddWithValue("@routeId", request.SmartRouteId);
            command.Parameters.AddWithValue("@hotelId", request.HotelId);
            command.Parameters.AddWithValue("@userId", userId);
            await command.ExecuteNonQueryAsync(cancellationToken);
            InvalidateCache();
            return (true, "Etikete katılımınız kaydedildi.");
        }

        const string leaveSql = """
            UPDATE [dbo].[AKILLI_ROTA_OTELLER]
            SET [AKTIF_MI] = 0,
                [BITIS_TARIHI] = sysutcdatetime(),
                [GUNCELLENME_TARIHI] = sysutcdatetime(),
                [KULLANICI_ID] = @userId
            WHERE [AKILLI_ROTA_ID] = @routeId
              AND [OTEL_ID] = @hotelId
              AND [AKTIF_MI] = 1;
            """;

        await using var leaveCommand = new SqlCommand(leaveSql, connection);
        leaveCommand.Parameters.AddWithValue("@routeId", request.SmartRouteId);
        leaveCommand.Parameters.AddWithValue("@hotelId", request.HotelId);
        leaveCommand.Parameters.AddWithValue("@userId", userId);
        var affected = await leaveCommand.ExecuteNonQueryAsync(cancellationToken);
        InvalidateCache();
        return affected > 0
            ? (true, "Etiketten ayrıldınız.")
            : (false, "Aktif bir katılım kaydı bulunamadı.");
    }

    private async Task<Dictionary<long, List<string>>> LoadHotelSlugMapAsync(SqlConnection connection, IReadOnlyList<long> hotelIds, CancellationToken cancellationToken)
    {
        var map = hotelIds.ToDictionary(id => id, _ => new List<string>());
        if (hotelIds.Count == 0)
        {
            return map;
        }

        var idList = string.Join(",", hotelIds);
        var sql = $"""
            SELECT
                aro.[OTEL_ID],
                ar.[ETIKET_KODU]
            FROM [dbo].[AKILLI_ROTA_OTELLER] aro
            INNER JOIN [dbo].[AKILLI_ROTA] ar ON ar.[ID] = aro.[AKILLI_ROTA_ID]
            WHERE aro.[OTEL_ID] IN ({idList})
              AND aro.[AKTIF_MI] = 1
              AND (aro.[BITIS_TARIHI] IS NULL OR aro.[BITIS_TARIHI] > sysutcdatetime())
              AND ar.[AKTIF_MI] = 1;
            """;

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var hotelId = reader.GetInt64(0);
            var slug = reader.GetString(1).Trim().ToLowerInvariant();
            if (map.TryGetValue(hotelId, out var slugs))
            {
                slugs.Add(slug);
            }
        }

        return map;
    }

    private static async Task<bool> HasHotelAccessAsync(SqlConnection connection, long userId, long hotelId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (1) 1
            FROM [dbo].[OTEL_KULLANICI_SAHIPLIKLERI] oks
            WHERE oks.[KULLANICI_ID] = @userId
              AND oks.[OTEL_ID] = @hotelId
              AND oks.[AKTIF_MI] = 1;
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@hotelId", hotelId);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null && result != DBNull.Value;
    }

    private static string NormalizeColorClass(string? value)
    {
        var normalized = (value ?? "sage").Trim().ToLowerInvariant();
        return normalized switch
        {
            "sage" or "amber" or "wine" or "sky" or "ocean" or "violet" => normalized,
            _ => "sage"
        };
    }

    private async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static async Task<bool> TableExistsAsync(SqlConnection connection, string tableName, CancellationToken cancellationToken)
    {
        const string sql = "SELECT CASE WHEN OBJECT_ID(@tableName, 'U') IS NOT NULL THEN 1 ELSE 0 END;";
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@tableName", $"dbo.{tableName}");
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) == 1;
    }
}
