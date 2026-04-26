using otelturizmnew.Models.Messages;
using otelturizmnew.Models.Firma;
using otelturizmnew.Models.Paneller.Firma;

namespace otelturizmnew.Services.Abstractions;

public interface IFirmaService
{
    Task<FirmaLandingPageViewModel> GetLandingPageAsync(CancellationToken cancellationToken = default);
    Task<FirmaDashboardPageViewModel> GetDashboardAsync(long userId, CancellationToken cancellationToken = default);
    Task<FirmaDealsPageViewModel> GetDealsAsync(long userId, string? city = null, int? minRoomCount = null, string? search = null, CancellationToken cancellationToken = default);
    Task<FirmaReservationsPageViewModel> GetReservationsAsync(long userId, CancellationToken cancellationToken = default);
    Task<FirmaMessagesPageViewModel> GetMessagesAsync(long userId, long? conversationId, CancellationToken cancellationToken = default);
    Task<FirmaEmployeesPageViewModel> GetEmployeesAsync(long userId, CancellationToken cancellationToken = default);
    Task<FirmaLimitsPageViewModel> GetLimitsAsync(long userId, CancellationToken cancellationToken = default);
    Task<FirmaInvoicesPageViewModel> GetInvoicesAsync(long userId, CancellationToken cancellationToken = default);
    Task<FirmaSpendingReportsPageViewModel> GetSpendingReportsAsync(long userId, CancellationToken cancellationToken = default);
    Task<FirmaHotelReportsPageViewModel> GetHotelReportsAsync(long userId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> CreateEmployeeAsync(long userId, FirmaEmployeeCreateModel model, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SendMessageAsync(long userId, MessageSendRequest form, IReadOnlyList<IFormFile>? attachments, HttpContext httpContext, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> DeleteMessageAsync(long userId, MessageDeleteRequest form, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UpsertLimitAsync(long userId, FirmaLimitUpsertModel model, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> UpdateReservationApprovalAsync(long userId, FirmaReservationDecisionModel model, CancellationToken cancellationToken = default);

    Task<FirmaCreateReservationPageViewModel> GetCreateReservationAsync(long userId, long? hotelId = null, long? roomTypeId = null, string? search = null, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message, long? ReservationId)> CreateReservationAsync(long userId, FirmaReservationCreateModel model, CancellationToken cancellationToken = default);
}
