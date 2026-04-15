using otelturizmnew.Models.Paneller.Common;

namespace otelturizmnew.Services.Abstractions;

public interface IHeaderBildiriService
{
    Task<HeaderBildiriViewModel> GetForPanelAsync(string panelKey, long userId, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(string panelKey, long userId, IReadOnlyCollection<string> itemKeys, CancellationToken cancellationToken = default);
}
