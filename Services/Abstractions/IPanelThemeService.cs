using otelturizmnew.Models.Paneller.Partner;

namespace otelturizmnew.Services.Abstractions;

public interface IPanelThemeService
{
    Task<PanelThemeViewModel> LoadAsync(string targetType, long targetId, CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> SaveAsync(string targetType, long targetId, PanelThemeViewModel theme, CancellationToken cancellationToken = default);
}

