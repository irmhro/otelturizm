using otelturizmnew.Models.OzelGun;

namespace otelturizmnew.Services.Abstractions;

public interface IOzelGunService
{
    Task<OzelGunTodayViewModel?> GetTodayAsync(CancellationToken cancellationToken = default);
}
