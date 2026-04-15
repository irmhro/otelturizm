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

    public async Task<IReadOnlyList<AddressCountryOption>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        const string tableCheckSql = """
            SELECT COUNT(*)
            FROM information_schema.TABLES
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = 'ulkeler';
            """;

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var tableCheck = new MySqlCommand(tableCheckSql, connection))
        {
            var exists = Convert.ToInt32(await tableCheck.ExecuteScalarAsync(cancellationToken), System.Globalization.CultureInfo.InvariantCulture) > 0;
            if (!exists)
            {
                return new List<AddressCountryOption>
                {
                    new() { Id = 1, Name = "Türkiye", Iso2 = "TR", Iso3 = "TUR" }
                };
            }
        }

        const string sql = """
            SELECT id, ulke_adi, iso2_kodu, iso3_kodu
            FROM ulkeler
            WHERE aktif_mi = 1
            ORDER BY varsayilan_ulke DESC, ulke_adi ASC;
            """;

        var items = new List<AddressCountryOption>();
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new AddressCountryOption
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Iso2 = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Iso3 = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
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

    public async Task<AddressSelectionResolution?> ResolveSelectionAsync(string? city, string? district, string? neighborhood, string? country, CancellationToken cancellationToken = default)
    {
        var result = new AddressSelectionResolution();
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(country))
        {
            const string countrySql = """
                SELECT id
                FROM ulkeler
                WHERE aktif_mi = 1
                  AND LOWER(ulke_adi) = LOWER(@country)
                LIMIT 1;
                """;
            try
            {
                await using var countryCommand = new MySqlCommand(countrySql, connection);
                countryCommand.Parameters.AddWithValue("@country", country.Trim());
                var countryId = await countryCommand.ExecuteScalarAsync(cancellationToken);
                if (countryId is not null && countryId != DBNull.Value)
                {
                    result.CountryId = Convert.ToInt64(countryId, System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            catch (MySqlException)
            {
                // ulkeler tablosu henüz yoksa fallback ile devam ediyoruz.
            }
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            const string provinceSql = """
                SELECT id
                FROM iller
                WHERE aktif_mi = 1
                  AND LOWER(il_adi) = LOWER(@city)
                LIMIT 1;
                """;
            await using var provinceCommand = new MySqlCommand(provinceSql, connection);
            provinceCommand.Parameters.AddWithValue("@city", city.Trim());
            var provinceId = await provinceCommand.ExecuteScalarAsync(cancellationToken);
            if (provinceId is not null && provinceId != DBNull.Value)
            {
                result.ProvinceId = Convert.ToInt64(provinceId, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        if (result.ProvinceId.HasValue && !string.IsNullOrWhiteSpace(district))
        {
            const string districtSql = """
                SELECT id
                FROM ilceler
                WHERE aktif_mi = 1
                  AND il_id = @provinceId
                  AND LOWER(ilce_adi) = LOWER(@district)
                LIMIT 1;
                """;
            await using var districtCommand = new MySqlCommand(districtSql, connection);
            districtCommand.Parameters.AddWithValue("@provinceId", result.ProvinceId.Value);
            districtCommand.Parameters.AddWithValue("@district", district.Trim());
            var districtId = await districtCommand.ExecuteScalarAsync(cancellationToken);
            if (districtId is not null && districtId != DBNull.Value)
            {
                result.DistrictId = Convert.ToInt64(districtId, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        if (result.DistrictId.HasValue && !string.IsNullOrWhiteSpace(neighborhood))
        {
            const string neighborhoodSql = """
                SELECT id
                FROM mahalleler
                WHERE aktif_mi = 1
                  AND ilce_id = @districtId
                  AND LOWER(mahalle_adi) = LOWER(@neighborhood)
                LIMIT 1;
                """;
            await using var neighborhoodCommand = new MySqlCommand(neighborhoodSql, connection);
            neighborhoodCommand.Parameters.AddWithValue("@districtId", result.DistrictId.Value);
            neighborhoodCommand.Parameters.AddWithValue("@neighborhood", neighborhood.Trim());
            var neighborhoodId = await neighborhoodCommand.ExecuteScalarAsync(cancellationToken);
            if (neighborhoodId is not null && neighborhoodId != DBNull.Value)
            {
                result.NeighborhoodId = Convert.ToInt64(neighborhoodId, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        return result;
    }
}
