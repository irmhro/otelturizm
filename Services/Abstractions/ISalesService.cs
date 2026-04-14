using otelturizmnew.Models.Paneller.Satis;

namespace otelturizmnew.Services.Abstractions;

public interface ISalesService
{
    Task<SalesDashboardPageViewModel> GetDashboardAsync(long userId, CancellationToken cancellationToken = default);
    Task<SalesCreateReservationPageViewModel> GetCreateReservationAsync(
        long userId,
        long? hotelId = null,
        long? roomTypeId = null,
        string? city = null,
        string? district = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? feature = null,
        CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, long? ReservationId)> CreateReservationAsync(long userId, SalesReservationCreateModel model, CancellationToken cancellationToken = default);
    Task<SalesCustomersPageViewModel> GetCustomersAsync(long userId, string? search = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> CreateCustomerAsync(long userId, SalesCustomerCreateModel model, CancellationToken cancellationToken = default);
    Task<SalesAvailabilityPageViewModel> GetAvailabilityAsync(long userId, long? hotelId = null, long? roomTypeId = null, DateOnly? month = null, CancellationToken cancellationToken = default);
    Task<SalesReservationsPageViewModel> GetReservationsAsync(long userId, CancellationToken cancellationToken = default);
    Task<SalesReportsPageViewModel> GetReportsAsync(long userId, CancellationToken cancellationToken = default);
    Task<SalesHotelGuidePageViewModel> GetHotelGuideAsync(long userId, string? search = null, CancellationToken cancellationToken = default);
}
