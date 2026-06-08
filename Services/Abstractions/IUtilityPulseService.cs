using otelturizmnew.Models.Anasayfa;

namespace otelturizmnew.Services.Abstractions;

public interface IUtilityPulseService
{
    Task<UtilityPulseBarViewModel> GetBarAsync(CancellationToken cancellationToken = default);
}
