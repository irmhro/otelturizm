using System.Data.Common;
using Microsoft.Data.SqlClient;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlCommand = Microsoft.Data.SqlClient.SqlCommand;
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

    public async Task<IReadOnlyList<AddressProvinceOption>> GetProvincesAsync(long countryId, CancellationToken cancellationToken = default)
    {
        if (countryId <= 0)
        {
            return Array.Empty<AddressProvinceOption>();
        }

        const string sql = """
            SELECT [ID], [ULKE_ID], [IL_ADI], [SEO_SLUG], [PLAKA_KODU], [BOLGE_TIPI], [ENLEM], [BOYLAM]
            FROM [dbo].[ILLER]
            WHERE [AKTIF_MI] = 1 AND [ULKE_ID] = @countryId
            ORDER BY
                CASE WHEN [BOLGE_TIPI] = N'IL' THEN [PLAKA_KODU] ELSE 999 END ASC,
                [IL_ADI] ASC;
            """;

        var items = new List<AddressProvinceOption>();
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(connection, sql);
        AddParameter(command, "@countryId", countryId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new AddressProvinceOption
            {
                Id = reader.GetInt64(0),
                CountryId = reader.GetInt64(1),
                Name = reader.GetString(2),
                Slug = reader.GetString(3),
                PlateCode = reader.GetInt16(4),
                RegionType = reader.IsDBNull(5) ? "IL" : reader.GetString(5).Trim(),
                Latitude = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                Longitude = reader.IsDBNull(7) ? null : reader.GetDecimal(7)
            });
        }

        return items;
    }

    public async Task<IReadOnlyList<AddressCountryOption>> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        const string tableCheckSql = """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_CATALOG = DB_NAME()
              AND TABLE_NAME = 'ULKELER';
            """;

        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        await using (var tableCheck = CreateCommand(connection, tableCheckSql))
        {
            var exists = Convert.ToInt32(await tableCheck.ExecuteScalarAsync(cancellationToken), System.Globalization.CultureInfo.InvariantCulture) > 0;
            if (!exists)
            {
                return new List<AddressCountryOption>
                {
                    new() { Id = 1, Name = "Türkiye", Iso2 = "TR", Iso3 = "TUR", FlagIconCode = "tr" }
                };
            }
        }

        const string sql = """
            SELECT [ID], [ULKE_ADI], [ISO2_KODU], [ISO3_KODU], [BAYRAK_IKON_KODU]
            FROM [dbo].[ULKELER]
            WHERE [AKTIF_MI] = 1
            ORDER BY [VARSAYILAN_ULKE] DESC, [ULKE_ADI] ASC;
            """;

        var items = new List<AddressCountryOption>();
        await using var command = CreateCommand(connection, sql);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var iso2 = reader.IsDBNull(2) ? string.Empty : reader.GetString(2).Trim();
            var flag = reader.IsDBNull(4) ? string.Empty : reader.GetString(4).Trim();
            if (string.IsNullOrWhiteSpace(flag) && !string.IsNullOrWhiteSpace(iso2))
            {
                flag = iso2.ToLowerInvariant();
            }

            items.Add(new AddressCountryOption
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Iso2 = iso2,
                Iso3 = reader.IsDBNull(3) ? string.Empty : reader.GetString(3).Trim(),
                FlagIconCode = flag
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
            SELECT [ID], [IL_ID], [ILCE_ADI], [SEO_SLUG], [ENLEM], [BOYLAM]
            FROM [dbo].[ILCELER]
            WHERE [AKTIF_MI] = 1 AND [IL_ID] = @provinceId
            ORDER BY [MERKEZ_MI] DESC, [ILCE_ADI] ASC;
            """;

        var items = new List<AddressDistrictOption>();
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(connection, sql);
        AddParameter(command, "@provinceId", provinceId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new AddressDistrictOption
            {
                Id = reader.GetInt64(0),
                ProvinceId = reader.GetInt64(1),
                Name = reader.GetString(2),
                Slug = reader.GetString(3),
                Latitude = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
                Longitude = reader.IsDBNull(5) ? null : reader.GetDecimal(5)
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
            SELECT [ID], [ILCE_ID], [MAHALLE_ADI], [SEO_SLUG], [POSTA_KODU], [ENLEM], [BOYLAM]
            FROM [dbo].[MAHALLELER]
            WHERE [AKTIF_MI] = 1 AND [ILCE_ID] = @districtId
            ORDER BY [MAHALLE_ADI] ASC;
            """;

        var items = new List<AddressNeighborhoodOption>();
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(connection, sql);
        AddParameter(command, "@districtId", districtId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new AddressNeighborhoodOption
            {
                Id = reader.GetInt64(0),
                DistrictId = reader.GetInt64(1),
                Name = reader.GetString(2),
                Slug = reader.GetString(3),
                PostalCode = reader.IsDBNull(4) ? null : reader.GetString(4),
                Latitude = reader.IsDBNull(5) ? null : reader.GetDecimal(5),
                Longitude = reader.IsDBNull(6) ? null : reader.GetDecimal(6)
            });
        }

        return items;
    }

    public async Task<AddressSelectionResolution?> ResolveSelectionAsync(string? city, string? district, string? neighborhood, string? country, CancellationToken cancellationToken = default)
    {
        var result = new AddressSelectionResolution();
        await using var connection = await CreateOpenConnectionAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(country))
        {
            var countrySql = """
                SELECT TOP (1) [ID]
                FROM [dbo].[ULKELER]
                WHERE [AKTIF_MI] = 1
                  AND LOWER([ULKE_ADI]) = LOWER(@country);
                """;
            try
            {
                await using var countryCommand = CreateCommand(connection, countrySql);
                AddParameter(countryCommand, "@country", country.Trim());
                var countryId = await countryCommand.ExecuteScalarAsync(cancellationToken);
                if (countryId is not null && countryId != DBNull.Value)
                {
                    result.CountryId = Convert.ToInt64(countryId, System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            catch (DbException)
            {
                // ULKELER tablosu henüz yoksa fallback ile devam ediyoruz.
            }
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var provinceSql = result.CountryId.HasValue
                ? """
                SELECT TOP (1) [ID]
                FROM [dbo].[ILLER]
                WHERE [AKTIF_MI] = 1
                  AND [ULKE_ID] = @countryId
                  AND LOWER([IL_ADI]) = LOWER(@city);
                """
                : """
                SELECT TOP (1) [ID]
                FROM [dbo].[ILLER]
                WHERE [AKTIF_MI] = 1
                  AND LOWER([IL_ADI]) = LOWER(@city);
                """;
            await using var provinceCommand = CreateCommand(connection, provinceSql);
            AddParameter(provinceCommand, "@city", city.Trim());
            if (result.CountryId.HasValue)
            {
                AddParameter(provinceCommand, "@countryId", result.CountryId.Value);
            }
            var provinceId = await provinceCommand.ExecuteScalarAsync(cancellationToken);
            if (provinceId is not null && provinceId != DBNull.Value)
            {
                result.ProvinceId = Convert.ToInt64(provinceId, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        if (result.ProvinceId.HasValue && !string.IsNullOrWhiteSpace(district))
        {
            var districtSql = """
                SELECT TOP (1) [ID]
                FROM [dbo].[ILCELER]
                WHERE [AKTIF_MI] = 1
                  AND [IL_ID] = @provinceId
                  AND LOWER([ILCE_ADI]) = LOWER(@district);
                """;
            await using var districtCommand = CreateCommand(connection, districtSql);
            AddParameter(districtCommand, "@provinceId", result.ProvinceId.Value);
            AddParameter(districtCommand, "@district", district.Trim());
            var districtId = await districtCommand.ExecuteScalarAsync(cancellationToken);
            if (districtId is not null && districtId != DBNull.Value)
            {
                result.DistrictId = Convert.ToInt64(districtId, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        if (result.DistrictId.HasValue && !string.IsNullOrWhiteSpace(neighborhood))
        {
            var neighborhoodSql = """
                SELECT TOP (1) [ID]
                FROM [dbo].[MAHALLELER]
                WHERE [AKTIF_MI] = 1
                  AND [ILCE_ID] = @districtId
                  AND LOWER([MAHALLE_ADI]) = LOWER(@neighborhood);
                """;
            await using var neighborhoodCommand = CreateCommand(connection, neighborhoodSql);
            AddParameter(neighborhoodCommand, "@districtId", result.DistrictId.Value);
            AddParameter(neighborhoodCommand, "@neighborhood", neighborhood.Trim());
            var neighborhoodId = await neighborhoodCommand.ExecuteScalarAsync(cancellationToken);
            if (neighborhoodId is not null && neighborhoodId != DBNull.Value)
            {
                result.NeighborhoodId = Convert.ToInt64(neighborhoodId, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        return result;
    }

    private async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        DbConnection connection = new SqlConnection(_connectionString);
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
