using otelturizmnew.Models.Paneller.Admin;

namespace otelturizmnew.Services.Abstractions;

public interface IAdminLocationService
{
    Task<AdminLocationListPageViewModel> GetCitiesPageAsync(
        string fullName,
        string email,
        string userRole,
        string? searchTerm = null,
        string? activeFilter = null,
        long? countryId = null,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default);

    Task<AdminLocationListPageViewModel> GetDistrictsPageAsync(
        string fullName,
        string email,
        string userRole,
        string? searchTerm = null,
        string? activeFilter = null,
        long? countryId = null,
        long? cityId = null,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default);

    Task<AdminLocationListPageViewModel> GetNeighborhoodsPageAsync(
        string fullName,
        string email,
        string userRole,
        string? searchTerm = null,
        string? activeFilter = null,
        long? countryId = null,
        long? cityId = null,
        long? districtId = null,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default);
}
