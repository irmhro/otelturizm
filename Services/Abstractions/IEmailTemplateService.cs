using otelturizmnew.Models.Email;

namespace otelturizmnew.Services.Abstractions;

public interface IEmailTemplateService
{
    Task<string> RenderTemplateFileAsync(string relativeViewPath, IReadOnlyDictionary<string, string> tokens, CancellationToken cancellationToken = default);
}
