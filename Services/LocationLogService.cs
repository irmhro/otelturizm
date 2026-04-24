using Microsoft.Data.SqlClient;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class LocationLogService : ILocationLogService
{
    private readonly IConfiguration _configuration;

    public LocationLogService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SaveUserLocationAsync(LocationLogEntryInput input, CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await EnsureLocationLogTableAsync(connection, cancellationToken);

        const string sql = """
            INSERT INTO kullanici_konum_loglari
            (
                user_id,
                session_key,
                enlem,
                boylam,
                yaricap_km,
                gorunen_otel_sayisi,
                listelenen_otel_idleri,
                arama_metni,
                arama_bolgesi,
                kaynak,
                kullanici_ajan,
                ip_adresi,
                cihaz_tipi,
                cihaz_modeli,
                platform,
                tarayici,
                telefon_ipucu,
                sayfa_url,
                kayit_tarihi
            )
            VALUES
            (
                @user_id,
                @session_key,
                @enlem,
                @boylam,
                @yaricap_km,
                @gorunen_otel_sayisi,
                @listelenen_otel_idleri,
                @arama_metni,
                @arama_bolgesi,
                @kaynak,
                @kullanici_ajan,
                @ip_adresi,
                @cihaz_tipi,
                @cihaz_modeli,
                @platform,
                @tarayici,
                @telefon_ipucu,
                @sayfa_url,
                SYSUTCDATETIME()
            );
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@user_id", input.UserId.HasValue && input.UserId.Value > 0 ? input.UserId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@session_key", DbOrString(input.SessionKey));
        command.Parameters.AddWithValue("@enlem", input.Latitude);
        command.Parameters.AddWithValue("@boylam", input.Longitude);
        command.Parameters.AddWithValue("@yaricap_km", input.RadiusKm.HasValue && input.RadiusKm.Value > 0 ? input.RadiusKm.Value : DBNull.Value);
        command.Parameters.AddWithValue("@gorunen_otel_sayisi", input.VisibleHotelCount.HasValue ? input.VisibleHotelCount.Value : DBNull.Value);
        command.Parameters.AddWithValue("@listelenen_otel_idleri", DbOrString(input.ListedHotelIds));
        command.Parameters.AddWithValue("@arama_metni", DbOrString(input.SearchTerm));
        command.Parameters.AddWithValue("@arama_bolgesi", DbOrString(input.SearchRegion));
        command.Parameters.AddWithValue("@kaynak", DbOrString(input.Source));
        command.Parameters.AddWithValue("@kullanici_ajan", DbOrString(input.UserAgent));
        command.Parameters.AddWithValue("@ip_adresi", DbOrString(input.IpAddress));
        command.Parameters.AddWithValue("@cihaz_tipi", DbOrString(input.DeviceType));
        command.Parameters.AddWithValue("@cihaz_modeli", DbOrString(input.DeviceModel));
        command.Parameters.AddWithValue("@platform", DbOrString(input.Platform));
        command.Parameters.AddWithValue("@tarayici", DbOrString(input.Browser));
        command.Parameters.AddWithValue("@telefon_ipucu", DbOrString(input.PhoneHint));
        command.Parameters.AddWithValue("@sayfa_url", DbOrString(input.PageUrl));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static object DbOrString(string? value)
        => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

    private static async Task EnsureLocationLogTableAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string existsSql = """
            SELECT CASE WHEN OBJECT_ID(N'dbo.kullanici_konum_loglari', N'U') IS NULL THEN 0 ELSE 1 END;
            """;

        await using (var existsCmd = new SqlCommand(existsSql, connection))
        {
            var existsObj = await existsCmd.ExecuteScalarAsync(cancellationToken);
            var exists = existsObj is not null && Convert.ToInt32(existsObj) == 1;
            if (!exists)
            {
                const string createSql = """
                    CREATE TABLE dbo.kullanici_konum_loglari
                    (
                        id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        user_id BIGINT NULL,
                        session_key NVARCHAR(64) NULL,
                        enlem DECIMAL(10,7) NOT NULL,
                        boylam DECIMAL(10,7) NOT NULL,
                        yaricap_km INT NULL,
                        gorunen_otel_sayisi INT NULL,
                        listelenen_otel_idleri NVARCHAR(MAX) NULL,
                        arama_metni NVARCHAR(256) NULL,
                        arama_bolgesi NVARCHAR(256) NULL,
                        kaynak NVARCHAR(64) NULL,
                        kullanici_ajan NVARCHAR(512) NULL,
                        ip_adresi NVARCHAR(64) NULL,
                        cihaz_tipi NVARCHAR(64) NULL,
                        cihaz_modeli NVARCHAR(64) NULL,
                        platform NVARCHAR(64) NULL,
                        tarayici NVARCHAR(64) NULL,
                        telefon_ipucu NVARCHAR(128) NULL,
                        sayfa_url NVARCHAR(512) NULL,
                        kayit_tarihi DATETIME2 NOT NULL CONSTRAINT DF_kullanici_konum_loglari_kayit_tarihi DEFAULT SYSUTCDATETIME()
                    );
                    """;

                await using var createCmd = new SqlCommand(createSql, connection);
                await createCmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        const string ensureColumnSql = """
            IF COL_LENGTH(N'dbo.kullanici_konum_loglari', N'listelenen_otel_idleri') IS NULL
            BEGIN
                ALTER TABLE dbo.kullanici_konum_loglari
                ADD listelenen_otel_idleri NVARCHAR(MAX) NULL;
            END
            """;

        await using var ensureColumnCmd = new SqlCommand(ensureColumnSql, connection);
        await ensureColumnCmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
