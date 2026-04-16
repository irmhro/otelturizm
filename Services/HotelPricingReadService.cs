using System.Globalization;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
using SqlTransaction = Microsoft.Data.SqlClient.SqlTransaction;
using SqlException = Microsoft.Data.SqlClient.SqlException;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class HotelPricingReadService : IHotelPricingReadService
{
    private readonly string _connectionString;
    private readonly bool _isSqlServer;

    public HotelPricingReadService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        var configuredProvider = configuration["Database:Provider"];
        _isSqlServer = string.Equals(configuredProvider, "SqlServer", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<decimal?> GetHotelEffectivePriceAsync(long hotelId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        if (hotelId <= 0)
        {
            return null;
        }

        var map = await GetHotelEffectivePriceMapAsync(new[] { hotelId }, startDate, endDate, cancellationToken);
        return map.TryGetValue(hotelId, out var price) ? price : null;
    }

    public async Task<IReadOnlyDictionary<long, decimal>> GetHotelEffectivePriceMapAsync(
        IReadOnlyCollection<long> hotelIds,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var ids = hotelIds
            .Where(static id => id > 0)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<long, decimal>();
        }

        var (fromDate, toDate) = NormalizeDateRange(startDate, endDate);
        var parameters = string.Join(", ", ids.Select((_, index) => $"@hotelId{index}"));
        var sql = $@"
            SELECT
                ot.otel_id,
                MIN(
                    CASE
                        WHEN ofm.kapali_satis = 1 THEN NULL
                        WHEN (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) <= 0 THEN NULL
                        WHEN ofm.gecelik_fiyat IS NULL OR ofm.gecelik_fiyat <= 0 THEN NULL
                        WHEN ofm.kampanya_id IS NOT NULL
                             AND ofm.kampanya_id > 0
                             AND ofm.indirimli_fiyat IS NOT NULL
                             AND ofm.indirimli_fiyat > 0
                             AND ofm.indirimli_fiyat < ofm.gecelik_fiyat
                            THEN ofm.indirimli_fiyat
                        ELSE ofm.gecelik_fiyat
                    END
                ) AS effective_price
            FROM oda_tipleri ot
            LEFT JOIN oda_fiyat_musaitlik ofm
                ON ofm.oda_tip_id = ot.id
               AND ofm.otel_id = ot.otel_id
               AND ofm.tarih BETWEEN @startDate AND @endDate
            WHERE ot.aktif_mi = 1
              AND ot.otel_id IN ({parameters})
            GROUP BY ot.otel_id;";

        var result = new Dictionary<long, decimal>();
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(connection, sql);
        AddParameter(command, "@startDate", fromDate.ToDateTime(TimeOnly.MinValue));
        AddParameter(command, "@endDate", toDate.ToDateTime(TimeOnly.MinValue));
        for (var i = 0; i < ids.Count; i++)
        {
            AddParameter(command, $"@hotelId{i}", ids[i]);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var hotelId = reader.GetInt64(0);
            if (reader.IsDBNull(1))
            {
                continue;
            }

            var price = reader.GetDecimal(1);
            if (price > 0m)
            {
                result[hotelId] = price;
            }
        }

        return result;
    }

    public async Task<decimal> GetRoomAverageNightlyPriceAsync(
        long roomTypeId,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        CancellationToken cancellationToken = default)
    {
        if (roomTypeId <= 0)
        {
            return 0m;
        }

        var map = await GetRoomAverageNightlyPriceMapAsync(new[] { roomTypeId }, checkInDate, checkOutDate, cancellationToken);
        return map.TryGetValue(roomTypeId, out var price) ? price : 0m;
    }

    public async Task<IReadOnlyDictionary<long, decimal>> GetRoomAverageNightlyPriceMapAsync(
        IReadOnlyCollection<long> roomTypeIds,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        CancellationToken cancellationToken = default)
    {
        var ids = roomTypeIds
            .Where(static id => id > 0)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<long, decimal>();
        }

        var startDate = checkInDate;
        var endDate = checkOutDate > checkInDate ? checkOutDate.AddDays(-1) : checkInDate;
        var (fromDate, toDate) = NormalizeDateRange(startDate, endDate);
        var parameters = string.Join(", ", ids.Select((_, index) => $"@roomTypeId{index}"));
        var sql = $@"
            SELECT
                ot.id,
                AVG(
                    CASE
                        WHEN ofm.kapali_satis = 1 THEN NULL
                        WHEN (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) <= 0 THEN NULL
                        WHEN ofm.gecelik_fiyat IS NULL OR ofm.gecelik_fiyat <= 0 THEN NULL
                        WHEN ofm.kampanya_id IS NOT NULL
                             AND ofm.kampanya_id > 0
                             AND ofm.indirimli_fiyat IS NOT NULL
                             AND ofm.indirimli_fiyat > 0
                             AND ofm.indirimli_fiyat < ofm.gecelik_fiyat
                            THEN ofm.indirimli_fiyat
                        ELSE ofm.gecelik_fiyat
                    END
                ) AS effective_nightly
            FROM oda_tipleri ot
            LEFT JOIN oda_fiyat_musaitlik ofm
                ON ofm.oda_tip_id = ot.id
               AND ofm.otel_id = ot.otel_id
               AND ofm.tarih BETWEEN @startDate AND @endDate
            WHERE ot.id IN ({parameters})
            GROUP BY ot.id;";

        var result = new Dictionary<long, decimal>();
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(connection, sql);
        AddParameter(command, "@startDate", fromDate.ToDateTime(TimeOnly.MinValue));
        AddParameter(command, "@endDate", toDate.ToDateTime(TimeOnly.MinValue));
        for (var i = 0; i < ids.Count; i++)
        {
            AddParameter(command, $"@roomTypeId{i}", ids[i]);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var roomId = reader.GetInt64(0);
            var value = reader.IsDBNull(1)
                ? 0m
                : Convert.ToDecimal(reader.GetValue(1), CultureInfo.InvariantCulture);
            if (value > 0m)
            {
                result[roomId] = value;
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<RoomNightlyPricePoint>> GetRoomNightlyBreakdownAsync(
        long roomTypeId,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        CancellationToken cancellationToken = default)
    {
        if (roomTypeId <= 0 || checkOutDate <= checkInDate)
        {
            return Array.Empty<RoomNightlyPricePoint>();
        }

        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        const string roomSql = @"
            SELECT TOP (1) otel_id, toplam_oda_sayisi
            FROM oda_tipleri
            WHERE id = @roomTypeId;";

        long roomHotelId;
        short defaultTotalRooms;
        await using (var roomCommand = CreateCommand(connection, roomSql))
        {
            AddParameter(roomCommand, "@roomTypeId", roomTypeId);
            await using var roomReader = await roomCommand.ExecuteReaderAsync(cancellationToken);
            if (!await roomReader.ReadAsync(cancellationToken))
            {
                return Array.Empty<RoomNightlyPricePoint>();
            }

            roomHotelId = roomReader.GetInt64(0);
            defaultTotalRooms = roomReader.IsDBNull(1)
                ? (short)0
                : Convert.ToInt16(roomReader.GetValue(1), CultureInfo.InvariantCulture);
        }

        const string pricingSql = @"
            SELECT tarih,
                   gecelik_fiyat,
                   indirimli_fiyat,
                   kampanya_id,
                   toplam_oda_sayisi,
                   satilan_oda_sayisi,
                   bloke_oda_sayisi,
                   kapali_satis
            FROM oda_fiyat_musaitlik
            WHERE oda_tip_id = @roomTypeId
              AND otel_id = @hotelId
              AND tarih BETWEEN @startDate AND @endDate
            ORDER BY tarih ASC;";

        var dailyOverrides = new Dictionary<DateOnly, (decimal BasePrice, decimal? DiscountPrice, short TotalRooms, short SoldRooms, short BlockedRooms, bool IsClosed)>();
        await using (var pricingCommand = CreateCommand(connection, pricingSql))
        {
            AddParameter(pricingCommand, "@roomTypeId", roomTypeId);
            AddParameter(pricingCommand, "@hotelId", roomHotelId);
            AddParameter(pricingCommand, "@startDate", checkInDate.ToDateTime(TimeOnly.MinValue));
            AddParameter(pricingCommand, "@endDate", checkOutDate.AddDays(-1).ToDateTime(TimeOnly.MinValue));
            await using var pricingReader = await pricingCommand.ExecuteReaderAsync(cancellationToken);
            while (await pricingReader.ReadAsync(cancellationToken))
            {
                var date = DateOnly.FromDateTime(pricingReader.GetDateTime(0));
                var basePrice = pricingReader.IsDBNull(1) ? 0m : pricingReader.GetDecimal(1);
                var rawDiscountPrice = pricingReader.IsDBNull(2) ? (decimal?)null : pricingReader.GetDecimal(2);
                var campaignId = pricingReader.IsDBNull(3) ? 0L : pricingReader.GetInt64(3);
                var validDiscountPrice = campaignId > 0
                    && rawDiscountPrice.HasValue
                    && rawDiscountPrice.Value > 0m
                    && basePrice > 0m
                    && rawDiscountPrice.Value < basePrice
                    ? rawDiscountPrice
                    : null;
                dailyOverrides[date] = (
                    basePrice,
                    validDiscountPrice,
                    pricingReader.IsDBNull(4) ? defaultTotalRooms : pricingReader.GetInt16(4),
                    pricingReader.IsDBNull(5) ? (short)0 : pricingReader.GetInt16(5),
                    pricingReader.IsDBNull(6) ? (short)0 : pricingReader.GetInt16(6),
                    !pricingReader.IsDBNull(7) && pricingReader.GetBoolean(7));
            }
        }

        var items = new List<RoomNightlyPricePoint>();
        for (var date = checkInDate; date < checkOutDate; date = date.AddDays(1))
        {
            if (!dailyOverrides.TryGetValue(date, out var day))
            {
                day = (0m, null, 0, 0, 0, false);
            }

            var remainingRooms = Math.Max(day.TotalRooms - day.SoldRooms - day.BlockedRooms, 0);
            var effectivePrice = day.DiscountPrice.HasValue && day.DiscountPrice.Value > 0m
                ? day.DiscountPrice.Value
                : day.BasePrice > 0m
                    ? day.BasePrice
                    : 0m;

            items.Add(new RoomNightlyPricePoint
            {
                Date = date,
                BasePrice = day.BasePrice,
                DiscountPrice = day.DiscountPrice,
                EffectivePrice = effectivePrice,
                RemainingRooms = Convert.ToInt16(Math.Min(remainingRooms, short.MaxValue), CultureInfo.InvariantCulture),
                IsClosed = day.IsClosed,
                IsAvailable = !day.IsClosed && remainingRooms > 0 && effectivePrice > 0m
            });
        }

        return items;
    }

    private static (DateOnly StartDate, DateOnly EndDate) NormalizeDateRange(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
        {
            return (endDate, startDate);
        }

        return (startDate, endDate);
    }

    private async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        DbConnection connection = _isSqlServer
            ? new SqlConnection(_connectionString)
            : new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static DbCommand CreateCommand(DbConnection connection, string sql)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        return command;
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
