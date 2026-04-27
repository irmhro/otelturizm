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

public sealed class SitemapDiagnosticsViewModel
{
    public DateTime? LastRefreshUtc { get; set; }
    public string MainSitemapPhysicalPath { get; set; } = string.Empty;
    public int MainSitemapUrlCount { get; set; }
    public int RegionalFileCount { get; set; }
    public int RegionalUrlCount { get; set; }
    public List<SitemapFileSummaryViewModel> Files { get; set; } = new();
}

public sealed class SitemapFileSummaryViewModel
{
    public string FileName { get; set; } = string.Empty;
    public string PhysicalPath { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;
    public string ScopeText { get; set; } = string.Empty;
    public DateTime? LastModifiedUtc { get; set; }
    public int UrlCount { get; set; }
}
