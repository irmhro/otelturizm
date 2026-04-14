namespace otelturizmnew.Services.Abstractions;

public interface IAddressLookupService
{
    Task<IReadOnlyList<AddressProvinceOption>> GetProvincesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AddressDistrictOption>> GetDistrictsAsync(long provinceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AddressNeighborhoodOption>> GetNeighborhoodsAsync(long districtId, CancellationToken cancellationToken = default);
}

public sealed class AddressProvinceOption
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public short PlateCode { get; set; }
}

public sealed class AddressDistrictOption
{
    public long Id { get; set; }
    public long ProvinceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public sealed class AddressNeighborhoodOption
{
    public long Id { get; set; }
    public long DistrictId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
}
