using otelturizmnew.Models;
using otelturizmnew.Models.Paneller.Admin;

namespace otelturizmnew.Services.Abstractions;

public interface IHotelCompletenessService
{
    HotelCompletenessSnapshot Evaluate(AdminHotelEditForm form, int roomCount, int hotelPhotoCount);
    Task<PartnerHotelCompletenessViewModel?> GetPartnerHotelCompletenessAsync(long hotelId, CancellationToken cancellationToken = default);
    Task<List<PartnerHotelCompletenessViewModel>> GetPartnerManagedHotelsCompletenessAsync(long userId, CancellationToken cancellationToken = default);
}
