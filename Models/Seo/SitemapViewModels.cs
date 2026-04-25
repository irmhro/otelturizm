namespace otelturizmnew.Models.Seo;

public sealed class SitemapUrlEntry
{
    public string Location { get; set; } = string.Empty;
    public DateTime? LastModifiedUtc { get; set; }
    public string ChangeFrequency { get; set; } = "weekly";
    public decimal Priority { get; set; } = 0.5m;
    public string? ImageUrl { get; set; }
    public string? ImageTitle { get; set; }
}
