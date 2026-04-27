namespace otelturizmnew.Models.Gelisim;

public sealed class DevelopmentPageViewModel
{
    public bool IsUnlocked { get; set; }
    public string Title { get; set; } = "Gelişim Planı";
    public string PasswordError { get; set; } = string.Empty;
    public string RenderedHtml { get; set; } = string.Empty;
    public DateTimeOffset? AccessExpiresAt { get; set; }
}
