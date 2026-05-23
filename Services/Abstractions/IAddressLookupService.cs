namespace otelturizmnew.Services.Abstractions;

public interface IAddressLookupService
{
    Task<IReadOnlyList<AddressCountryOption>> GetCountriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AddressProvinceOption>> GetProvincesAsync(long countryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AddressDistrictOption>> GetDistrictsAsync(long provinceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AddressNeighborhoodOption>> GetNeighborhoodsAsync(long districtId, CancellationToken cancellationToken = default);
    Task<AddressSelectionResolution?> ResolveSelectionAsync(string? city, string? district, string? neighborhood, string? country, CancellationToken cancellationToken = default);
}

public sealed class AddressCountryOption
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Iso2 { get; set; } = string.Empty;
    public string Iso3 { get; set; } = string.Empty;
    public string FlagIconCode { get; set; } = string.Empty;
}

public sealed class AddressProvinceOption
{
    public long Id { get; set; }
    public long CountryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string RegionType { get; set; } = "IL";
    public short PlateCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

public sealed class AddressDistrictOption
{
    public long Id { get; set; }
    public long ProvinceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

public sealed class AddressNeighborhoodOption
{
    public long Id { get; set; }
    public long DistrictId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

public sealed class AddressSelectionResolution
{
    public long? CountryId { get; set; }
    public long? ProvinceId { get; set; }
    public long? DistrictId { get; set; }
    public long? NeighborhoodId { get; set; }
}
