using System.Globalization;
using MySqlConnector;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class HotelPricingReadService : IHotelPricingReadService
{
    private readonly string _connectionString;

    public HotelPricingReadService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
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
                    COALESCE(
                        CASE
                            WHEN ofm.kapali_satis = 1 THEN NULL
                            WHEN (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) <= 0 THEN NULL
                            ELSE COALESCE(NULLIF(ofm.indirimli_fiyat, 0), NULLIF(ofm.gecelik_fiyat, 0))
                        END,
                        NULLIF(ot.standart_gecelik_fiyat, 0)
                    )
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
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@startDate", fromDate.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@endDate", toDate.ToDateTime(TimeOnly.MinValue));
        for (var i = 0; i < ids.Count; i++)
        {
            command.Parameters.AddWithValue($"@hotelId{i}", ids[i]);
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
                COALESCE(
                    AVG(
                        CASE
                            WHEN ofm.kapali_satis = 1 THEN NULL
                            WHEN (COALESCE(ofm.toplam_oda_sayisi, ot.toplam_oda_sayisi) - COALESCE(ofm.satilan_oda_sayisi, 0) - COALESCE(ofm.bloke_oda_sayisi, 0)) <= 0 THEN NULL
                            ELSE COALESCE(NULLIF(ofm.indirimli_fiyat, 0), NULLIF(ofm.gecelik_fiyat, 0))
                        END
                    ),
                    NULLIF(ot.standart_gecelik_fiyat, 0),
                    0
                ) AS effective_nightly
            FROM oda_tipleri ot
            LEFT JOIN oda_fiyat_musaitlik ofm
                ON ofm.oda_tip_id = ot.id
               AND ofm.otel_id = ot.otel_id
               AND ofm.tarih BETWEEN @startDate AND @endDate
            WHERE ot.id IN ({parameters})
            GROUP BY ot.id, ot.standart_gecelik_fiyat;";

        var result = new Dictionary<long, decimal>();
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@startDate", fromDate.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@endDate", toDate.ToDateTime(TimeOnly.MinValue));
        for (var i = 0; i < ids.Count; i++)
        {
            command.Parameters.AddWithValue($"@roomTypeId{i}", ids[i]);
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

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        const string roomSql = @"
            SELECT otel_id, standart_gecelik_fiyat, toplam_oda_sayisi
            FROM oda_tipleri
            WHERE id = @roomTypeId
            LIMIT 1;";

        long roomHotelId;
        decimal defaultBasePrice;
        short defaultTotalRooms;
        await using (var roomCommand = new MySqlCommand(roomSql, connection))
        {
            roomCommand.Parameters.AddWithValue("@roomTypeId", roomTypeId);
            await using var roomReader = await roomCommand.ExecuteReaderAsync(cancellationToken);
            if (!await roomReader.ReadAsync(cancellationToken))
            {
                return Array.Empty<RoomNightlyPricePoint>();
            }

            roomHotelId = roomReader.GetInt64(0);
            defaultBasePrice = roomReader.IsDBNull(1)
                ? 0m
                : Convert.ToDecimal(roomReader.GetValue(1), CultureInfo.InvariantCulture);
            defaultTotalRooms = roomReader.IsDBNull(2)
                ? (short)0
                : Convert.ToInt16(roomReader.GetValue(2), CultureInfo.InvariantCulture);
        }

        const string pricingSql = @"
            SELECT tarih,
                   gecelik_fiyat,
                   indirimli_fiyat,
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
        await using (var pricingCommand = new MySqlCommand(pricingSql, connection))
        {
            pricingCommand.Parameters.AddWithValue("@roomTypeId", roomTypeId);
            pricingCommand.Parameters.AddWithValue("@hotelId", roomHotelId);
            pricingCommand.Parameters.AddWithValue("@startDate", checkInDate.ToDateTime(TimeOnly.MinValue));
            pricingCommand.Parameters.AddWithValue("@endDate", checkOutDate.AddDays(-1).ToDateTime(TimeOnly.MinValue));
            await using var pricingReader = await pricingCommand.ExecuteReaderAsync(cancellationToken);
            while (await pricingReader.ReadAsync(cancellationToken))
            {
                var date = DateOnly.FromDateTime(pricingReader.GetDateTime(0));
                dailyOverrides[date] = (
                    pricingReader.IsDBNull(1) ? defaultBasePrice : pricingReader.GetDecimal(1),
                    pricingReader.IsDBNull(2) ? null : pricingReader.GetDecimal(2),
                    pricingReader.IsDBNull(3) ? defaultTotalRooms : pricingReader.GetInt16(3),
                    pricingReader.IsDBNull(4) ? (short)0 : pricingReader.GetInt16(4),
                    pricingReader.IsDBNull(5) ? (short)0 : pricingReader.GetInt16(5),
                    !pricingReader.IsDBNull(6) && pricingReader.GetBoolean(6));
            }
        }

        var items = new List<RoomNightlyPricePoint>();
        for (var date = checkInDate; date < checkOutDate; date = date.AddDays(1))
        {
            if (!dailyOverrides.TryGetValue(date, out var day))
            {
                day = (defaultBasePrice, null, defaultTotalRooms, 0, 0, false);
            }

            var remainingRooms = Math.Max(day.TotalRooms - day.SoldRooms - day.BlockedRooms, 0);
            var effectivePrice = day.DiscountPrice.HasValue && day.DiscountPrice.Value > 0m
                ? day.DiscountPrice.Value
                : day.BasePrice > 0m
                    ? day.BasePrice
                    : defaultBasePrice;

            items.Add(new RoomNightlyPricePoint
            {
                Date = date,
                BasePrice = day.BasePrice > 0m ? day.BasePrice : defaultBasePrice,
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
}
