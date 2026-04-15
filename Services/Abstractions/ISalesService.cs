using otelturizmnew.Models.Paneller.Satis;

namespace otelturizmnew.Services.Abstractions;

public interface ISalesService
{
    Task<SalesDashboardPageViewModel> GetDashboardAsync(long userId, CancellationToken cancellationToken = default);
    Task<SalesCreateReservationPageViewModel> GetCreateReservationAsync(
        long userId,
        long? hotelId = null,
        long? roomTypeId = null,
        long? customerId = null,
        string? searchTerm = null,
        string? city = null,
        string? district = null,
        string? neighborhood = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        decimal? minimumRating = null,
        int? minimumReviewCount = null,
        string? feature = null,
        CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, long? ReservationId)> CreateReservationAsync(long userId, SalesReservationCreateModel model, CancellationToken cancellationToken = default);
    Task<SalesCustomersPageViewModel> GetCustomersAsync(long userId, string? search = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> CreateCustomerAsync(long userId, SalesCustomerCreateModel model, CancellationToken cancellationToken = default);
    Task<SalesAvailabilityPageViewModel> GetAvailabilityAsync(long userId, long? hotelId = null, long? roomTypeId = null, string? search = null, DateOnly? month = null, CancellationToken cancellationToken = default);
    Task<SalesReservationsPageViewModel> GetReservationsAsync(long userId, SalesReservationsFilterViewModel filters, CancellationToken cancellationToken = default);
    Task<SalesReportsPageViewModel> GetReportsAsync(long userId, int year, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<SalesHotelGuidePageViewModel> GetHotelGuideAsync(long userId, string? search = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SalesHotelSearchCardViewModel>> SearchHotelsForAssistantAsync(
        long userId,
        string? searchTerm = null,
        string? city = null,
        string? district = null,
        string? neighborhood = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        decimal? minimumRating = null,
        int? minimumReviewCount = null,
        string? feature = null,
        int resultLimit = 8,
        CancellationToken cancellationToken = default);
}
