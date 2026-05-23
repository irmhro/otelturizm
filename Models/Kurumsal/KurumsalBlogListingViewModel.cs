namespace otelturizmnew.Models.Kurumsal;

public sealed class KurumsalBlogListingViewModel
{
    public List<KurumsalBlogCardViewModel> Posts { get; set; } = new();
}

public sealed class KurumsalBlogCardViewModel
{
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? HeroImageUrl { get; init; }
    public string LinkUrl { get; init; } = string.Empty;
}
