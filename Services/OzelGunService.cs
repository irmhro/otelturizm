using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using otelturizmnew.Models.OzelGun;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class OzelGunService : IOzelGunService
{
    private const string AllRowsCacheKey = "ozel-gun:rows:v1";
    private static readonly TimeSpan RowsCacheDuration = TimeSpan.FromHours(6);

    private readonly string _connectionString;
    private readonly IMemoryCache _cache;
    private readonly ITimeZoneService _timeZoneService;

    public OzelGunService(IConfiguration configuration, IMemoryCache cache, ITimeZoneService timeZoneService)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _cache = cache;
        _timeZoneService = timeZoneService;
    }

    public async Task<OzelGunTodayViewModel?> GetTodayAsync(CancellationToken cancellationToken = default)
    {
        var localToday = DateOnly.FromDateTime(_timeZoneService.ToLocal(DateTime.UtcNow));
        var cacheKey = $"ozel-gun:today:{localToday:yyyy-MM-dd}";

        if (_cache.TryGetValue(cacheKey, out OzelGunTodayViewModel? cached))
        {
            return cached;
        }

        var rows = await GetActiveRowsAsync(cancellationToken);
        var match = rows
            .Where(row => MatchesDate(row, localToday))
            .OrderBy(row => row.Siralama)
            .ThenBy(row => row.GunAdi, StringComparer.CurrentCulture)
            .FirstOrDefault();

        OzelGunTodayViewModel? model = match is null
            ? null
            : new OzelGunTodayViewModel
            {
                GunKodu = match.GunKodu,
                GunAdi = match.GunAdi,
                KutlamaMetni = match.KutlamaMetni,
                Emoji = match.Emoji,
                Kategori = match.Kategori
            };

        var expiresAt = _timeZoneService.ToLocal(DateTime.UtcNow).Date.AddDays(1);
        var ttl = expiresAt - _timeZoneService.ToLocal(DateTime.UtcNow);
        if (ttl < TimeSpan.FromMinutes(1))
        {
            ttl = TimeSpan.FromMinutes(1);
        }

        _cache.Set(cacheKey, model, ttl);
        return model;
    }

    public void InvalidateCache()
    {
        _cache.Remove(AllRowsCacheKey);
        var localToday = DateOnly.FromDateTime(_timeZoneService.ToLocal(DateTime.UtcNow));
        _cache.Remove($"ozel-gun:today:{localToday:yyyy-MM-dd}");
    }

    private async Task<IReadOnlyList<OzelGunRow>> GetActiveRowsAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(AllRowsCacheKey, out IReadOnlyList<OzelGunRow>? cachedRows) && cachedRows is not null)
        {
            return cachedRows;
        }

        const string sql = """
            SELECT
                [GUN_KODU],
                [GUN_ADI],
                [AY],
                [GUN],
                [KURAL_TIPI],
                [KURAL_PARAM1],
                [KURAL_PARAM2],
                [EMOJI],
                [KUTLAMA_METNI],
                [KATEGORI],
                [SIRALAMA]
            FROM [dbo].[OZEL_GUNLER]
            WHERE [AKTIF_MI] = 1
            ORDER BY [SIRALAMA], [GUN_ADI];
            """;

        var rows = new List<OzelGunRow>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new OzelGunRow
            {
                GunKodu = reader.GetString(0),
                GunAdi = reader.GetString(1),
                Ay = reader.GetByte(2),
                Gun = reader.IsDBNull(3) ? null : reader.GetByte(3),
                KuralTipi = reader.GetString(4),
                KuralParam1 = reader.IsDBNull(5) ? null : reader.GetByte(5),
                KuralParam2 = reader.IsDBNull(6) ? null : reader.GetByte(6),
                Emoji = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                KutlamaMetni = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                Kategori = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                Siralama = reader.GetInt32(10)
            });
        }

        IReadOnlyList<OzelGunRow> snapshot = rows;
        _cache.Set(AllRowsCacheKey, snapshot, RowsCacheDuration);
        return snapshot;
    }

    private static bool MatchesDate(OzelGunRow row, DateOnly date)
    {
        if (date.Month != row.Ay)
        {
            return false;
        }

        return row.KuralTipi switch
        {
            "SABIT" => row.Gun.HasValue && row.Gun.Value == date.Day,
            "NINCI_HAFTA_GUNU" => MatchesNthWeekday(date, row),
            _ => false
        };
    }

    private static bool MatchesNthWeekday(DateOnly date, OzelGunRow row)
    {
        if (!row.KuralParam1.HasValue || !row.KuralParam2.HasValue)
        {
            return false;
        }

        var weekday = (DayOfWeek)row.KuralParam2.Value;
        var target = GetNthWeekdayOfMonth(date.Year, date.Month, row.KuralParam1.Value, weekday);
        return target == date;
    }

    private static DateOnly GetNthWeekdayOfMonth(int year, int month, int nth, DayOfWeek weekday)
    {
        if (nth < 1)
        {
            nth = 1;
        }

        var cursor = new DateOnly(year, month, 1);
        var found = 0;
        while (cursor.Month == month)
        {
            if (cursor.DayOfWeek == weekday)
            {
                found++;
                if (found == nth)
                {
                    return cursor;
                }
            }

            cursor = cursor.AddDays(1);
        }

        return new DateOnly(year, month, DateTime.DaysInMonth(year, month));
    }

    private sealed class OzelGunRow
    {
        public string GunKodu { get; init; } = string.Empty;
        public string GunAdi { get; init; } = string.Empty;
        public byte Ay { get; init; }
        public byte? Gun { get; init; }
        public string KuralTipi { get; init; } = "SABIT";
        public byte? KuralParam1 { get; init; }
        public byte? KuralParam2 { get; init; }
        public string Emoji { get; init; } = string.Empty;
        public string KutlamaMetni { get; init; } = string.Empty;
        public string Kategori { get; init; } = string.Empty;
        public int Siralama { get; init; }
    }
}
