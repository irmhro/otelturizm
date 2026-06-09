namespace otelturizmnew.Models.Anasayfa;

public class AnasayfaViewModel
{
    public string HeroTitle { get; set; } = "Hayalindeki Oteli Kesfet";
    public string HeroDescription { get; set; } = "Turkiye'nin en iyi sehir otelleri, butik tesisleri ve haftasonu kacamaklari tek platformda.";
    public List<HomeHeroSlideViewModel> HeroSlides { get; set; } = new();
    public List<HomeCampaignSlideViewModel> CampaignSlides { get; set; } = new();
    public List<HomeDestinationCardViewModel> PopularDestinations { get; set; } = new();
    public List<HomeHotelCardViewModel> PopularHotels { get; set; } = new();
    public List<HomeHotelCardViewModel> WeekendHotels { get; set; } = new();
    public List<HomeHotelCardViewModel> FeaturedRouteHotels { get; set; } = new();
    public List<HomeCategorySectionViewModel> CustomHomepageSections { get; set; } = new();
    public List<HomeCategorySectionViewModel> CategorySections { get; set; } = new();
}

public class HomeCategorySectionViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Etiket { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<HomeHotelCardViewModel> Hotels { get; set; } = new();
}

public class HomeHotelSectionRenderModel
{
    public string SectionKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string SeeAllUrl { get; set; } = "/oteller";
    public List<HomeHotelCardViewModel> Hotels { get; set; } = new();
    public string[] FallbackImages { get; set; } = Array.Empty<string>();
    public bool AllowEmptyFallback { get; set; }
}

public class HomeHeroSlideViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string DetailSlug { get; set; } = string.Empty;
    // Optional weather info for the hero slide (icon class & temperature string)
    public string WeatherIcon { get; set; } = string.Empty; // e.g. "fa-cloud-sun"
    public string Temperature { get; set; } = string.Empty; // e.g. "18°C"
}

public class HomeCampaignSlideViewModel
{
    public string CampaignName { get; set; } = string.Empty;
    public string Slogan { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public string DetailUrl { get; set; } = string.Empty;
    public string BadgeText { get; set; } = string.Empty;
}

public class HomeDestinationCardViewModel
{
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public int HotelCount { get; set; }
    public string LeadText { get; set; } = string.Empty;
    public List<string> RecentHotelNames { get; set; } = new();
    public string ImageUrl { get; set; } = string.Empty;
    public string ListingUrl { get; set; } = string.Empty;
}

public class HomeHotelCardViewModel
{
    public long Id { get; set; }
    public string HotelCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string LocationText { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public string RatingText { get; set; } = "Yorum Bekleniyor";
    public int ReviewCount { get; set; }
    public decimal? StartingPrice { get; set; }
    public decimal? OriginalPrice { get; set; }
    public decimal? DiscountedPrice { get; set; }
    public int DiscountPercent { get; set; }
    public bool HasDiscount { get; set; }
    public long? DiscountId { get; set; }
    public string? DiscountName { get; set; }
    public string? DiscountShortDescription { get; set; }
    public string? DiscountImageUrl { get; set; }
    public string PriceText { get; set; } = "Fiyat icin detay sayfasina bakin";
    public string PriceNote { get; set; } = "GÜNLÜK · vergi öncesi";
    public string ImageUrl { get; set; } = string.Empty;
    public List<string> GalleryImageUrls { get; set; } = new();
    public string DetailSlug { get; set; } = string.Empty;
    public List<HomeAmenityViewModel> Amenities { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<string> RecentCampaigns { get; set; } = new();
    public List<string> RecentDiscounts { get; set; } = new();
    public bool IsSmartPrice { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsRecommended { get; set; }
    public bool IsFavorite { get; set; }
    public byte? StarCount { get; set; }
    // Weather info for display on card
    public string WeatherIcon { get; set; } = ""; // font-awesome icon class e.g. 'fa-cloud-sun'
    public string Temperature { get; set; } = ""; // e.g. '18°C'
}

public class HomeAmenityViewModel
{
    public string Label { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-circle-check";
}

