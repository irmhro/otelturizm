using otelturizmnew.Models.Oteller;

namespace otelturizmnew.Models.Kampanyalar;

public class CampaignListingPageViewModel
{
    public string PageTitle { get; set; } = "Kampanyalar";
    public string PageDescription { get; set; } = "Aktif kampanyalar ve kampanyaya dahil oteller burada listelenir.";
    public List<CampaignCardViewModel> Campaigns { get; set; } = new();
}

public class CampaignCardViewModel
{
    public long CampaignId { get; set; }
    public string CampaignCode { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public string? BadgeText { get; set; }
    public string? PromoBadge { get; set; }
    public string? HeroImageUrl { get; set; }
    public string ColorCode { get; set; } = "#003B95";
    public int HotelCount { get; set; }
    public bool IsFeatured { get; set; }
}

public class CampaignDetailPageViewModel
{
    public long CampaignId { get; set; }
    public string CampaignCode { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string CanonicalUrl { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string LongDescription { get; set; } = string.Empty;
    public string ListingTitle { get; set; } = string.Empty;
    public string ListingDescription { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public string? Terms { get; set; }
    public string? BadgeText { get; set; }
    public string? PromoBadge { get; set; }
    public string? HeroImageUrl { get; set; }
    public string? CardImageUrl { get; set; }
    public string ColorCode { get; set; } = "#003B95";
    public int HotelCount { get; set; }
    public bool IsFeatured { get; set; }
    public List<HotelListingCardViewModel> Hotels { get; set; } = new();
}
