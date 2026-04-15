using System.Text;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly IWebHostEnvironment _environment;

    public EmailTemplateService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> RenderTemplateFileAsync(string relativeViewPath, IReadOnlyDictionary<string, string> tokens, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativeViewPath))
        {
            throw new InvalidOperationException("E-posta şablon yolu boş.");
        }

        var normalized = relativeViewPath.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        var absolutePath = Path.Combine(_environment.ContentRootPath, normalized);
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException($"E-posta şablonu bulunamadı: {absolutePath}");
        }

        var content = await File.ReadAllTextAsync(absolutePath, Encoding.UTF8, cancellationToken);
        foreach (var token in tokens)
        {
            var key = token.Key.StartsWith("{{", StringComparison.Ordinal) ? token.Key : $"{{{{{token.Key}}}}}";
            content = content.Replace(key, token.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return content;
    }
}
