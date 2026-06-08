using System.Globalization;
using Microsoft.Data.SqlClient;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class UserLoyaltyPointsService : IUserLoyaltyPointsService
{
    private readonly string _connectionString;
    private readonly ILogger<UserLoyaltyPointsService> _logger;

    public UserLoyaltyPointsService(IConfiguration configuration, ILogger<UserLoyaltyPointsService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
        _logger = logger;
    }

    public async Task<bool> TryAwardReservationPointsAsync(
        long userId,
        long reservationId,
        int points = 4,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || reservationId <= 0 || points <= 0)
        {
            return false;
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await EnsureLoyaltyAccountAsync(connection, userId, cancellationToken);

        await using (var existsCommand = new SqlCommand(@"
            SELECT COUNT(*)
            FROM [dbo].[KULLANICI_PUAN_HAREKETLERI]
            WHERE [REZERVASYON_ID] = @reservationId
              AND [HAREKET_TIPI] = N'Rezervasyon';", connection))
        {
            existsCommand.Parameters.AddWithValue("@reservationId", reservationId);
            var exists = Convert.ToInt32(await existsCommand.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
            if (exists > 0)
            {
                return false;
            }
        }

        long accountId;
        var currentBalance = 0;
        await using (var accountCommand = new SqlCommand(@"
            SELECT TOP (1) id, [KULLANILABILIR_PUAN]
            FROM [dbo].[KULLANICI_SADAKAT_HESAPLARI]
            WHERE [KULLANICI_ID] = @userId;", connection))
        {
            accountCommand.Parameters.AddWithValue("@userId", userId);
            await using var reader = await accountCommand.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return false;
            }

            accountId = reader.GetInt64(0);
            currentBalance = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
        }

        var newBalance = currentBalance + points;
        await using (var insertCommand = new SqlCommand(@"
            INSERT INTO [dbo].[KULLANICI_PUAN_HAREKETLERI]
            ([KULLANICI_ID], [SADAKAT_HESAP_ID], [REZERVASYON_ID], [HAREKET_TIPI], [BASLIK], [ACIKLAMA], [PUAN_DEGISIM], [PUAN_BAKIYE_SONRASI], [DURUM], [ISLEM_TARIHI], [OLUSTURULMA_TARIHI])
            VALUES
            (@userId, @accountId, @reservationId, N'Rezervasyon', N'Rezervasyon puani', @description, @delta, @newBalance, N'Tamamlandi', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);", connection))
        {
            insertCommand.Parameters.AddWithValue("@userId", userId);
            insertCommand.Parameters.AddWithValue("@accountId", accountId);
            insertCommand.Parameters.AddWithValue("@reservationId", reservationId);
            insertCommand.Parameters.AddWithValue("@description", $"Rezervasyon #{reservationId}");
            insertCommand.Parameters.AddWithValue("@delta", points);
            insertCommand.Parameters.AddWithValue("@newBalance", newBalance);
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await SyncLoyaltyAccountAsync(connection, userId, cancellationToken);
        _logger.LogInformation("LOYALTY_POINTS reservation userId={UserId} reservationId={ReservationId} points={Points}", userId, reservationId, points);
        return true;
    }

    private async Task EnsureLoyaltyAccountAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        await using (var existsCommand = new SqlCommand(
                         "SELECT COUNT(*) FROM [dbo].[KULLANICI_SADAKAT_HESAPLARI] WHERE [KULLANICI_ID] = @userId;", connection))
        {
            existsCommand.Parameters.AddWithValue("@userId", userId);
            var exists = Convert.ToInt32(await existsCommand.ExecuteScalarAsync(cancellationToken) ?? 0, CultureInfo.InvariantCulture);
            if (exists > 0)
            {
                return;
            }
        }

        var bronzeTierId = await ResolveTierIdAsync(connection, "BRONZE", cancellationToken);
        var silverTierId = await ResolveTierIdAsync(connection, "SILVER", cancellationToken);
        await using var insertCommand = new SqlCommand(@"
            INSERT INTO [dbo].[KULLANICI_SADAKAT_HESAPLARI]
            ([KULLANICI_ID], [TOPLAM_PUAN], [KULLANILABILIR_PUAN], [BU_YIL_KAZANILAN_PUAN], [BU_YIL_KULLANILAN_PUAN], [MEVCUT_SEVIYE_ID], [SONRAKI_SEVIYE_ID], [PUAN_GECERLILIK_TARIHI], [OLUSTURULMA_TARIHI], [GUNCELLENME_TARIHI])
            VALUES
            (@userId, 0, 0, 0, 0, @currentTierId, @nextTierId, DATEADD(DAY, 365, CAST(GETDATE() AS date)), CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);", connection);
        insertCommand.Parameters.AddWithValue("@userId", userId);
        insertCommand.Parameters.AddWithValue("@currentTierId", bronzeTierId);
        insertCommand.Parameters.AddWithValue("@nextTierId", silverTierId);
        await insertCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task SyncLoyaltyAccountAsync(SqlConnection connection, long userId, CancellationToken cancellationToken)
    {
        await using var syncCommand = new SqlCommand(@"
            UPDATE h
            SET h.[TOPLAM_PUAN] = agg.kazanilan,
                h.[KULLANILABILIR_PUAN] = CASE WHEN agg.kazanilan - agg.kullanilan > 0 THEN agg.kazanilan - agg.kullanilan ELSE 0 END,
                h.[BU_YIL_KAZANILAN_PUAN] = yearly.yil_kazanilan,
                h.[BU_YIL_KULLANILAN_PUAN] = yearly.yil_kullanilan,
                h.[MEVCUT_SEVIYE_ID] = COALESCE(current_tier.id, h.[MEVCUT_SEVIYE_ID]),
                h.[SONRAKI_SEVIYE_ID] = next_tier.id,
                h.[SON_SEVIYE_GUNCELLEME_TARIHI] = CURRENT_TIMESTAMP,
                h.[GUNCELLENME_TARIHI] = CURRENT_TIMESTAMP
            FROM [dbo].[KULLANICI_SADAKAT_HESAPLARI] h
            CROSS APPLY (
                SELECT
                    COALESCE(SUM(CASE WHEN p.[PUAN_DEGISIM] > 0 THEN p.[PUAN_DEGISIM] ELSE 0 END), 0) AS kazanilan,
                    COALESCE(ABS(SUM(CASE WHEN p.[PUAN_DEGISIM] < 0 THEN p.[PUAN_DEGISIM] ELSE 0 END)), 0) AS kullanilan
                FROM [dbo].[KULLANICI_PUAN_HAREKETLERI] p
                WHERE p.[KULLANICI_ID] = @userId
                  AND COALESCE(p.[DURUM], 'Tamamlandi') <> 'Iptal'
            ) agg
            CROSS APPLY (
                SELECT
                    COALESCE(SUM(CASE WHEN y.[PUAN_DEGISIM] > 0 THEN y.[PUAN_DEGISIM] ELSE 0 END), 0) AS yil_kazanilan,
                    COALESCE(ABS(SUM(CASE WHEN y.[PUAN_DEGISIM] < 0 THEN y.[PUAN_DEGISIM] ELSE 0 END)), 0) AS yil_kullanilan
                FROM [dbo].[KULLANICI_PUAN_HAREKETLERI] y
                WHERE y.[KULLANICI_ID] = @userId
                  AND YEAR(COALESCE(y.[ISLEM_TARIHI], CURRENT_TIMESTAMP)) = YEAR(CAST(GETDATE() AS date))
            ) yearly
            OUTER APPLY (
                SELECT TOP (1) s.id
                FROM [dbo].[SADAKAT_SEVIYELERI] s
                WHERE agg.kazanilan - agg.kullanilan >= s.[MINIMUM_PUAN]
                  AND (s.[MAXIMUM_PUAN] IS NULL OR agg.kazanilan - agg.kullanilan <= s.[MAXIMUM_PUAN])
                ORDER BY s.[MINIMUM_PUAN] DESC
            ) current_tier
            OUTER APPLY (
                SELECT TOP (1) s2.id
                FROM [dbo].[SADAKAT_SEVIYELERI] s2
                WHERE s2.[MINIMUM_PUAN] > agg.kazanilan - agg.kullanilan
                ORDER BY s2.[MINIMUM_PUAN] ASC
            ) next_tier
            WHERE h.[KULLANICI_ID] = @userId;", connection);
        syncCommand.Parameters.AddWithValue("@userId", userId);
        await syncCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<long> ResolveTierIdAsync(SqlConnection connection, string code, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("SELECT TOP (1) id FROM [dbo].[SADAKAT_SEVIYELERI] WHERE kod = @code;", connection);
        command.Parameters.AddWithValue("@code", code);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null ? 0L : Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }
}
