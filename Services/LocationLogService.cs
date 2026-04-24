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

        const string sql = """
            INSERT INTO kullanici_konum_loglari
            (
                user_id,
                session_key,
                enlem,
                boylam,
                yaricap_km,
                gorunen_otel_sayisi,
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

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@user_id", input.UserId.HasValue && input.UserId.Value > 0 ? input.UserId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@session_key", DbOrString(input.SessionKey));
        command.Parameters.AddWithValue("@enlem", input.Latitude);
        command.Parameters.AddWithValue("@boylam", input.Longitude);
        command.Parameters.AddWithValue("@yaricap_km", input.RadiusKm.HasValue && input.RadiusKm.Value > 0 ? input.RadiusKm.Value : DBNull.Value);
        command.Parameters.AddWithValue("@gorunen_otel_sayisi", input.VisibleHotelCount.HasValue ? input.VisibleHotelCount.Value : DBNull.Value);
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
}
