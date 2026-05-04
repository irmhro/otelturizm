using Microsoft.AspNetCore.Http;

namespace otelturizmnew.Models.DeveloperFeedback;

public sealed class DeveloperFeedbackForm
{
    public long? FeedbackId { get; set; }
    public string PanelKey { get; set; } = string.Empty;
    public string FeedbackType { get; set; } = "Geliştirme";
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string PageUrl { get; set; } = string.Empty;
    public string PageTitle { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = "/";
    public string Viewport { get; set; } = string.Empty;
    public string DeviceInfo { get; set; } = string.Empty;
    public IFormFile? Screenshot { get; set; }
}
