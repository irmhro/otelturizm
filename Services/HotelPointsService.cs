using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class HotelPointsService : IHotelPointsService
{
    private const string CacheKeyKazanim = "HotelPoints:Rules:KAZANIM";
    private const string CacheKeyIndirim = "HotelPoints:Rules:INDIRIM";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly string _connectionString;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<HotelPointsService> _logger;

    public HotelPointsService(IConfiguration configuration, IMemoryCache memoryCache, ILogger<HotelPointsService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public void InvalidateCache()
    {
        _memoryCache.Remove(CacheKeyKazanim);
        _memoryCache.Remove(CacheKeyIndirim);
    }

    public int CalculateEarnPoints(decimal totalAmount)
    {
        if (totalAmount <= 0m)
        {
            return 0;
        }

        var rules = GetCachedRules(CacheKeyKazanim);
        foreach (var rule in rules)
        {
            if (MatchesRange(totalAmount, rule.MinDeger, rule.MaxDeger))
            {
                return rule.PuanDegeri ?? 0;
            }
        }

        return 0;
    }

    public decimal? CalculateDiscountPercent(int availablePoints)
    {
        if (availablePoints <= 0)
        {
            return null;
        }

        var rules = GetCachedRules(CacheKeyIndirim);
        decimal? best = null;
        foreach (var rule in rules)
        {
            if (MatchesRange(availablePoints, rule.MinDeger, rule.MaxDeger))
            {
                best = rule.IndirimYuzde;
            }
        }

        return best;
    }

    public int? EstimateFromNightlyPrice(decimal? nightlyPrice)
    {
        if (nightlyPrice is not > 0m)
        {
            return null;
        }

        var points = CalculateEarnPoints(nightlyPrice.Value);
        return points > 0 ? points : null;
    }

    public async Task<bool> TryAwardReservationPointsAsync(
        long userId,
        long hotelId,
        long reservationId,
        decimal totalAmount,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || hotelId <= 0 || reservationId <= 0)
        {
            return false;
        }

        var points = CalculateEarnPoints(totalAmount);
        if (points <= 0)
        {
            return false;
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        if (!await TableExistsAsync(connection, "PUAN_KULLANICI", cancellationToken))
        {
            _logger.LogWarning("PUAN_KULLANICI tablosu bulunamadi. Migration uygulayin.");
            return false;
        }

        await using (var existsCommand = new SqlCommand(@"
            SELECT COUNT(*)
            FROM [dbo].[KULLANICI_PUAN_HAREKETLERI]
            WHERE [REZERVASYON_ID] = @reservationId
              AND [OTEL_ID] = @hotelId
              AND [HAREKET_TIPI] = N'OtelRezervasyon';", connection))
        {
            existsCommand.Parameters.AddWithValue("@reservationId", reservationId);
            existsCommand.Parameters.AddWithValue("@hotelId", hotelId);
            var exists = Convert.ToInt32(await existsCommand.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
            if (exists > 0)
            {
                return false;
            }
        }

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var balanceId = await EnsureUserHotelBalanceAsync(connection, transaction, userId, hotelId, cancellationToken);
            var newBalance = await ApplyPointDeltaAsync(
                connection,
                transaction,
                balanceId,
                userId,
                hotelId,
                points,
                isEarn: true,
                cancellationToken);

            await using (var insertCommand = new SqlCommand(@"
                INSERT INTO [dbo].[KULLANICI_PUAN_HAREKETLERI]
                    ([KULLANICI_ID], [SADAKAT_HESAP_ID], [REZERVASYON_ID], [OTEL_ID], [HAREKET_TIPI], [BASLIK], [ACIKLAMA], [PUAN_DEGISIM], [PUAN_BAKIYE_SONRASI], [DURUM], [ISLEM_TARIHI], [OLUSTURULMA_TARIHI])
                VALUES
                    (@userId, NULL, @reservationId, @hotelId, N'OtelRezervasyon', N'Otel rezervasyon puani', @description, @delta, @newBalance, N'Tamamlandi', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);", connection, transaction))
            {
                insertCommand.Parameters.AddWithValue("@userId", userId);
                insertCommand.Parameters.AddWithValue("@reservationId", reservationId);
                insertCommand.Parameters.AddWithValue("@hotelId", hotelId);
                insertCommand.Parameters.AddWithValue("@description", $"Rezervasyon #{reservationId} · {totalAmount:N2} TL");
                insertCommand.Parameters.AddWithValue("@delta", points);
                insertCommand.Parameters.AddWithValue("@newBalance", newBalance);
                await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation(
                "HOTEL_POINTS reservation userId={UserId} hotelId={HotelId} reservationId={ReservationId} points={Points}",
                userId, hotelId, reservationId, points);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<(bool Success, string Message)> AdjustUserHotelPointsAsync(
        long userId,
        long hotelId,
        int pointDelta,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || hotelId <= 0 || pointDelta == 0)
        {
            return (false, "Geçersiz kullanıcı, otel veya puan değeri.");
        }

        var note = (reason ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(note))
        {
            return (false, "Açıklama zorunludur.");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        if (!await TableExistsAsync(connection, "PUAN_KULLANICI", cancellationToken))
        {
            return (false, "PUAN_KULLANICI tablosu bulunamadı. Migration uygulayın.");
        }

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var balanceId = await EnsureUserHotelBalanceAsync(connection, transaction, userId, hotelId, cancellationToken);

            if (pointDelta < 0)
            {
                await using var balanceCommand = new SqlCommand(
                    "SELECT [KULLANILABILIR_PUAN] FROM [dbo].[PUAN_KULLANICI] WHERE [ID] = @id;",
                    connection,
                    transaction);
                balanceCommand.Parameters.AddWithValue("@id", balanceId);
                var current = Convert.ToInt32(await balanceCommand.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
                if (current + pointDelta < 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return (false, $"Yetersiz bakiye. Mevcut: {current} puan.");
                }
            }

            var newBalance = await ApplyPointDeltaAsync(
                connection,
                transaction,
                balanceId,
                userId,
                hotelId,
                pointDelta,
                isEarn: pointDelta > 0,
                cancellationToken);

            await using (var insertCommand = new SqlCommand(@"
                INSERT INTO [dbo].[KULLANICI_PUAN_HAREKETLERI]
                    ([KULLANICI_ID], [SADAKAT_HESAP_ID], [REZERVASYON_ID], [OTEL_ID], [HAREKET_TIPI], [BASLIK], [ACIKLAMA], [PUAN_DEGISIM], [PUAN_BAKIYE_SONRASI], [DURUM], [ISLEM_TARIHI], [OLUSTURULMA_TARIHI])
                VALUES
                    (@userId, NULL, NULL, @hotelId, N'OtelManuel', N'Manuel otel puani', @description, @delta, @newBalance, N'Tamamlandi', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);", connection, transaction))
            {
                insertCommand.Parameters.AddWithValue("@userId", userId);
                insertCommand.Parameters.AddWithValue("@hotelId", hotelId);
                insertCommand.Parameters.AddWithValue("@description", note);
                insertCommand.Parameters.AddWithValue("@delta", pointDelta);
                insertCommand.Parameters.AddWithValue("@newBalance", newBalance);
                await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return (true, pointDelta > 0 ? $"{pointDelta} puan eklendi." : $"{Math.Abs(pointDelta)} puan düşüldü.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogWarning(ex, "Manuel otel puani duzenlenemedi userId={UserId} hotelId={HotelId}", userId, hotelId);
            return (false, "Puan düzenleme sırasında hata oluştu.");
        }
    }

    public async Task<IReadOnlyList<UserHotelPointsBalance>> GetUserHotelBalancesAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            return Array.Empty<UserHotelPointsBalance>();
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        if (!await TableExistsAsync(connection, "PUAN_KULLANICI", cancellationToken))
        {
            return Array.Empty<UserHotelPointsBalance>();
        }

        const string sql = """
            SELECT
                pk.[OTEL_ID],
                COALESCE(o.[OTEL_ADI], N'Otel') AS hotel_name,
                COALESCE(o.[SEHIR], N'') AS city_name,
                pk.[TOPLAM_KAZANILAN],
                pk.[KULLANILABILIR_PUAN],
                pk.[KULLANILAN_PUAN],
                pk.[SON_KAZANIM_TARIHI],
                COALESCE(rs.stay_count, 0) AS stay_count,
                rs.last_checkout
            FROM [dbo].[PUAN_KULLANICI] pk
            INNER JOIN [dbo].[OTELLER] o ON o.[ID] = pk.[OTEL_ID]
            LEFT JOIN (
                SELECT
                    r.[OTEL_ID],
                    COUNT(*) AS stay_count,
                    MAX(r.[CIKIS_TARIHI]) AS last_checkout
                FROM [dbo].[REZERVASYONLAR] r
                WHERE r.[KULLANICI_ID] = @userId
                  AND COALESCE(NULLIF(r.[DURUM], ''), '') NOT IN (N'İptal Edildi', N'Reddedildi')
                GROUP BY r.[OTEL_ID]
            ) rs ON rs.[OTEL_ID] = pk.[OTEL_ID]
            WHERE pk.[KULLANICI_ID] = @userId
              AND (pk.[TOPLAM_KAZANILAN] > 0 OR pk.[KULLANILABILIR_PUAN] > 0 OR pk.[KULLANILAN_PUAN] > 0)
            ORDER BY pk.[KULLANILABILIR_PUAN] DESC, pk.[SON_KAZANIM_TARIHI] DESC;
            """;

        var balances = new List<UserHotelPointsBalance>();
        var hotelIds = new List<long>();
        await using (var command = new SqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@userId", userId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var hotelId = reader.GetInt64(0);
                hotelIds.Add(hotelId);
                var available = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
                var stayCount = reader.IsDBNull(7) ? 0 : reader.GetInt32(7);
                var lastCheckout = reader.IsDBNull(8) ? (DateTime?)null : reader.GetDateTime(8);
                balances.Add(new UserHotelPointsBalance
                {
                    HotelId = hotelId,
                    HotelName = reader.IsDBNull(1) ? "Otel" : reader.GetString(1),
                    HotelCity = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    TotalEarned = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                    AvailablePoints = available,
                    UsedPoints = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                    DiscountPercent = CalculateDiscountPercent(available),
                    LastEarnedText = reader.IsDBNull(6) ? null : reader.GetDateTime(6).ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                    StayCount = stayCount,
                    LastStayText = lastCheckout.HasValue
                        ? lastCheckout.Value.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR"))
                        : null
                });
            }
        }

        if (hotelIds.Count == 0)
        {
            return balances;
        }

        Dictionary<long, List<UserHotelPointMovement>> movements;
        try
        {
            movements = await LoadRecentMovementsAsync(connection, userId, hotelIds, cancellationToken);
        }
        catch (SqlException ex)
        {
            _logger.LogWarning(ex, "Otel puan hareketleri okunamadi userId={UserId}", userId);
            movements = new Dictionary<long, List<UserHotelPointMovement>>();
        }

        for (var i = 0; i < balances.Count; i++)
        {
            var balance = balances[i];
            if (!movements.TryGetValue(balance.HotelId, out var rows))
            {
                continue;
            }

            balances[i] = new UserHotelPointsBalance
            {
                HotelId = balance.HotelId,
                HotelName = balance.HotelName,
                HotelCity = balance.HotelCity,
                TotalEarned = balance.TotalEarned,
                AvailablePoints = balance.AvailablePoints,
                UsedPoints = balance.UsedPoints,
                DiscountPercent = balance.DiscountPercent,
                LastEarnedText = balance.LastEarnedText,
                StayCount = balance.StayCount,
                LastStayText = balance.LastStayText,
                RecentMovements = rows
            };
        }

        return balances;
    }

    public async Task<IReadOnlyList<PartnerDistributedPointsRow>> GetPartnerDistributedPointsAsync(
        long partnerUserId,
        long? hotelId = null,
        CancellationToken cancellationToken = default)
    {
        if (partnerUserId <= 0)
        {
            return Array.Empty<PartnerDistributedPointsRow>();
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        if (!await TableExistsAsync(connection, "PUAN_KULLANICI", cancellationToken))
        {
            return Array.Empty<PartnerDistributedPointsRow>();
        }

        var sql = """
            SELECT
                pk.[OTEL_ID],
                COALESCE(o.[OTEL_ADI], N'Otel') AS hotel_name,
                pk.[KULLANICI_ID],
                COALESCE(u.[AD_SOYAD], N'Misafir') AS user_name,
                pk.[TOPLAM_KAZANILAN],
                pk.[KULLANILABILIR_PUAN],
                pk.[KULLANILAN_PUAN],
                pk.[SON_KAZANIM_TARIHI]
            FROM [dbo].[PUAN_KULLANICI] pk
            INNER JOIN [dbo].[OTELLER] o ON o.[ID] = pk.[OTEL_ID]
            INNER JOIN [dbo].[OTEL_KULLANICI_SAHIPLIKLERI] oks ON oks.[OTEL_ID] = pk.[OTEL_ID] AND oks.[AKTIF_MI] = 1
            INNER JOIN [dbo].[KULLANICILAR] u ON u.[ID] = pk.[KULLANICI_ID]
            WHERE oks.[KULLANICI_ID] = @partnerUserId
              AND pk.[TOPLAM_KAZANILAN] > 0
            """;

        if (hotelId is > 0)
        {
            sql += " AND pk.[OTEL_ID] = @hotelId";
        }

        sql += " ORDER BY pk.[SON_KAZANIM_TARIHI] DESC, pk.[TOPLAM_KAZANILAN] DESC;";

        var rows = new List<PartnerDistributedPointsRow>();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@partnerUserId", partnerUserId);
        if (hotelId is > 0)
        {
            command.Parameters.AddWithValue("@hotelId", hotelId.Value);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new PartnerDistributedPointsRow
            {
                HotelId = reader.GetInt64(0),
                HotelName = reader.IsDBNull(1) ? "Otel" : reader.GetString(1),
                UserId = reader.GetInt64(2),
                UserDisplayName = reader.IsDBNull(3) ? "Misafir" : reader.GetString(3),
                TotalEarned = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                AvailablePoints = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                UsedPoints = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                LastEarnedText = reader.IsDBNull(7) ? null : reader.GetDateTime(7).ToString("dd MMM yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"))
            });
        }

        return rows;
    }

    private IReadOnlyList<PuanAyarRule> GetCachedRules(string cacheKey)
    {
        var ayarTipi = cacheKey == CacheKeyKazanim ? "KAZANIM" : "INDIRIM";
        try
        {
            return _memoryCache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return LoadRulesAsync(ayarTipi, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }) ?? DefaultRules(ayarTipi);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PUAN_AYAR kurallari yuklenemedi; varsayilan kurallar kullaniliyor tip={AyarTipi}", ayarTipi);
            _memoryCache.Remove(cacheKey);
            return DefaultRules(ayarTipi);
        }
    }

    private async Task<IReadOnlyList<PuanAyarRule>> LoadRulesAsync(string ayarTipi, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        if (!await TableExistsAsync(connection, "PUAN_AYAR", cancellationToken))
        {
            return DefaultRules(ayarTipi);
        }

        const string sql = """
            SELECT [MIN_DEGER], [MAX_DEGER], [PUAN_DEGERI], [INDIRIM_YUZDE], [SIRA_NO]
            FROM [dbo].[PUAN_AYAR]
            WHERE [AYAR_TIPI] = @tip AND [AKTIF_MI] = 1
            ORDER BY [SIRA_NO], [MIN_DEGER];
            """;

        var rules = new List<PuanAyarRule>();
        try
        {
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@tip", ayarTipi);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                rules.Add(new PuanAyarRule(
                    reader.GetDecimal(0),
                    reader.IsDBNull(1) ? null : reader.GetDecimal(1),
                    reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                    reader.GetInt32(4)));
            }
        }
        catch (SqlException ex)
        {
            _logger.LogWarning(ex, "PUAN_AYAR sorgusu basarisiz; varsayilan kurallar kullaniliyor tip={AyarTipi}", ayarTipi);
            return DefaultRules(ayarTipi);
        }

        return rules.Count > 0 ? rules : DefaultRules(ayarTipi);
    }

    private static IReadOnlyList<PuanAyarRule> DefaultRules(string ayarTipi)
        => string.Equals(ayarTipi, "INDIRIM", StringComparison.OrdinalIgnoreCase)
            ? new[]
            {
                new PuanAyarRule(10, 24, null, 2, 10),
                new PuanAyarRule(25, 49, null, 5, 20),
                new PuanAyarRule(50, 99, null, 8, 30),
                new PuanAyarRule(100, null, null, 12, 40)
            }
            : new[]
            {
                new PuanAyarRule(0, 2000, 5, null, 10),
                new PuanAyarRule(2001, 5000, 10, null, 20),
                new PuanAyarRule(5001, 10000, 20, null, 30),
                new PuanAyarRule(10001, null, 35, null, 40)
            };

    private static bool MatchesRange(decimal value, decimal minDeger, decimal? maxDeger)
        => value >= minDeger && (maxDeger is null || value <= maxDeger.Value);

    private static async Task<long> EnsureUserHotelBalanceAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long userId,
        long hotelId,
        CancellationToken cancellationToken)
    {
        await using (var existsCommand = new SqlCommand(@"
            SELECT TOP (1) [ID]
            FROM [dbo].[PUAN_KULLANICI]
            WHERE [KULLANICI_ID] = @userId AND [OTEL_ID] = @hotelId;", connection, transaction))
        {
            existsCommand.Parameters.AddWithValue("@userId", userId);
            existsCommand.Parameters.AddWithValue("@hotelId", hotelId);
            var existing = await existsCommand.ExecuteScalarAsync(cancellationToken);
            if (existing is not null)
            {
                return Convert.ToInt64(existing, CultureInfo.InvariantCulture);
            }
        }

        await using var insertCommand = new SqlCommand(@"
            INSERT INTO [dbo].[PUAN_KULLANICI]
                ([KULLANICI_ID], [OTEL_ID], [TOPLAM_KAZANILAN], [KULLANILABILIR_PUAN], [KULLANILAN_PUAN])
            OUTPUT INSERTED.[ID]
            VALUES (@userId, @hotelId, 0, 0, 0);", connection, transaction);
        insertCommand.Parameters.AddWithValue("@userId", userId);
        insertCommand.Parameters.AddWithValue("@hotelId", hotelId);
        var inserted = await insertCommand.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(inserted ?? 0L, CultureInfo.InvariantCulture);
    }

    private static async Task<int> ApplyPointDeltaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long balanceId,
        long userId,
        long hotelId,
        int pointDelta,
        bool isEarn,
        CancellationToken cancellationToken)
    {
        var earnSql = isEarn
            ? "[TOPLAM_KAZANILAN] = [TOPLAM_KAZANILAN] + @delta, [KULLANILABILIR_PUAN] = [KULLANILABILIR_PUAN] + @delta, [SON_KAZANIM_TARIHI] = CURRENT_TIMESTAMP,"
            : "[KULLANILAN_PUAN] = [KULLANILAN_PUAN] + ABS(@delta), [KULLANILABILIR_PUAN] = [KULLANILABILIR_PUAN] + @delta, [SON_KULLANIM_TARIHI] = CURRENT_TIMESTAMP,";

        await using var updateCommand = new SqlCommand($@"
            UPDATE [dbo].[PUAN_KULLANICI]
            SET {earnSql}
                [GUNCELLENME_TARIHI] = CURRENT_TIMESTAMP
            OUTPUT INSERTED.[KULLANILABILIR_PUAN]
            WHERE [ID] = @id AND [KULLANICI_ID] = @userId AND [OTEL_ID] = @hotelId;", connection, transaction);
        updateCommand.Parameters.AddWithValue("@id", balanceId);
        updateCommand.Parameters.AddWithValue("@userId", userId);
        updateCommand.Parameters.AddWithValue("@hotelId", hotelId);
        updateCommand.Parameters.AddWithValue("@delta", pointDelta);
        var result = await updateCommand.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result ?? 0, CultureInfo.InvariantCulture);
    }

    private static async Task<Dictionary<long, List<UserHotelPointMovement>>> LoadRecentMovementsAsync(
        SqlConnection connection,
        long userId,
        IReadOnlyList<long> hotelIds,
        CancellationToken cancellationToken)
    {
        if (!await ColumnExistsAsync(connection, "KULLANICI_PUAN_HAREKETLERI", "OTEL_ID", cancellationToken))
        {
            return hotelIds.ToDictionary(static id => id, static _ => new List<UserHotelPointMovement>());
        }

        var map = hotelIds.ToDictionary(static id => id, static _ => new List<UserHotelPointMovement>());
        var idList = string.Join(",", hotelIds);
        var sql = $@"
            SELECT [OTEL_ID], [BASLIK], [ACIKLAMA], [PUAN_DEGISIM], [ISLEM_TARIHI]
            FROM (
                SELECT
                    h.[OTEL_ID], h.[BASLIK], h.[ACIKLAMA], h.[PUAN_DEGISIM], h.[ISLEM_TARIHI],
                    ROW_NUMBER() OVER (PARTITION BY h.[OTEL_ID] ORDER BY h.[ISLEM_TARIHI] DESC) AS rn
                FROM [dbo].[KULLANICI_PUAN_HAREKETLERI] h
                WHERE h.[KULLANICI_ID] = @userId
                  AND h.[OTEL_ID] IN ({idList})
                  AND COALESCE(h.[DURUM], N'Tamamlandi') <> N'Iptal'
            ) x
            WHERE x.rn <= 5
            ORDER BY x.[ISLEM_TARIHI] DESC;";

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var hotelId = reader.GetInt64(0);
            if (!map.TryGetValue(hotelId, out var list))
            {
                continue;
            }

            var delta = reader.GetInt32(3);
            list.Add(new UserHotelPointMovement
            {
                DateText = reader.GetDateTime(4).ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("tr-TR")),
                Title = reader.IsDBNull(1) ? "Puan hareketi" : reader.GetString(1),
                Description = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                PointChange = delta,
                PointChangeText = delta > 0 ? $"+{delta}" : delta.ToString(CultureInfo.InvariantCulture)
            });
        }

        return map.ToDictionary(static kv => kv.Key, static kv => (List<UserHotelPointMovement>)kv.Value);
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

    private static async Task<bool> ColumnExistsAsync(
        SqlConnection connection,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(@"
            SELECT COUNT(*)
            FROM information_schema.COLUMNS
            WHERE TABLE_CATALOG = DB_NAME()
              AND TABLE_NAME = @tableName
              AND COLUMN_NAME = @columnName;", connection);
        command.Parameters.AddWithValue("@tableName", tableName);
        command.Parameters.AddWithValue("@columnName", columnName);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, CultureInfo.InvariantCulture) > 0;
    }

    private sealed record PuanAyarRule(decimal MinDeger, decimal? MaxDeger, int? PuanDegeri, decimal? IndirimYuzde, int SiraNo);
}
