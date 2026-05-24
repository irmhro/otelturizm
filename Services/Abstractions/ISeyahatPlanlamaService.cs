using otelturizmnew.Models.SeyahatPlanlama;

namespace otelturizmnew.Services.Abstractions;

public interface ISeyahatPlanlamaService
{
    Task<SeyahatPlanlamaPageViewModel> BuildPageAsync(
        string? destinationKey,
        int? nights,
        decimal? budgetTry,
        bool budgetSubmitted,
        bool isAuthenticated,
        CancellationToken cancellationToken = default);
}
