namespace otelturizmnew.Models.Seo;

public sealed class SitemapUrlEntry
{
    public string Location { get; set; } = string.Empty;
    public DateTime? LastModifiedUtc { get; set; }
    public string ChangeFrequency { get; set; } = "weekly";
    public decimal Priority { get; set; } = 0.5m;
    public string? ImageUrl { get; set; }
    public string? ImageTitle { get; set; }
    public List<SitemapImageEntry> Images { get; set; } = new();
    public bool IncludeHrefLang { get; set; } = true;
}

public sealed class SitemapImageEntry
{
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Caption { get; set; }
}

public sealed class SitemapDiagnosticsViewModel
{
    public DateTime? LastRefreshUtc { get; set; }
    public string MainSitemapPhysicalPath { get; set; } = string.Empty;
    public int MainSitemapUrlCount { get; set; }
    public int SubSitemapCount { get; set; }
    public int HotelUrlCount { get; set; }
    public int RoomUrlCount { get; set; }
    public int CampaignUrlCount { get; set; }
    public int BlogUrlCount { get; set; }
    public int LocationUrlCount { get; set; }
    public int RegionalFileCount { get; set; }
    public int RegionalUrlCount { get; set; }
    public int PriceFeedOfferCount { get; set; }
    public string PriceFeedUrl { get; set; } = string.Empty;
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
