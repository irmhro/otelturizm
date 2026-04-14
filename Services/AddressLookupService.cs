using MySqlConnector;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class AddressLookupService : IAddressLookupService
{
    private readonly string _connectionString;

    public AddressLookupService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection tanimli degil.");
    }

    public async Task<IReadOnlyList<AddressProvinceOption>> GetProvincesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, il_adi, seo_slug, plaka_kodu
            FROM iller
            WHERE aktif_mi = 1
            ORDER BY plaka_kodu ASC, il_adi ASC;
            """;

        var items = new List<AddressProvinceOption>();
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new AddressProvinceOption
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Slug = reader.GetString(2),
                PlateCode = reader.GetInt16(3)
            });
        }

        return items;
    }

    public async Task<IReadOnlyList<AddressDistrictOption>> GetDistrictsAsync(long provinceId, CancellationToken cancellationToken = default)
    {
        if (provinceId <= 0)
        {
            return Array.Empty<AddressDistrictOption>();
        }

        const string sql = """
            SELECT id, il_id, ilce_adi, seo_slug
            FROM ilceler
            WHERE aktif_mi = 1 AND il_id = @provinceId
            ORDER BY merkez_mi DESC, ilce_adi ASC;
            """;

        var items = new List<AddressDistrictOption>();
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@provinceId", provinceId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new AddressDistrictOption
            {
                Id = reader.GetInt64(0),
                ProvinceId = reader.GetInt64(1),
                Name = reader.GetString(2),
                Slug = reader.GetString(3)
            });
        }

        return items;
    }

    public async Task<IReadOnlyList<AddressNeighborhoodOption>> GetNeighborhoodsAsync(long districtId, CancellationToken cancellationToken = default)
    {
        if (districtId <= 0)
        {
            return Array.Empty<AddressNeighborhoodOption>();
        }

        const string sql = """
            SELECT id, ilce_id, mahalle_adi, seo_slug, posta_kodu
            FROM mahalleler
            WHERE aktif_mi = 1 AND ilce_id = @districtId
            ORDER BY mahalle_adi ASC;
            """;

        var items = new List<AddressNeighborhoodOption>();
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@districtId", districtId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new AddressNeighborhoodOption
            {
                Id = reader.GetInt64(0),
                DistrictId = reader.GetInt64(1),
                Name = reader.GetString(2),
                Slug = reader.GetString(3),
                PostalCode = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }

        return items;
    }
}
