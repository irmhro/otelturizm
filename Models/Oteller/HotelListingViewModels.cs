namespace otelturizmnew.Models.Oteller;

public class HotelListingPageViewModel
{
    public string City { get; set; } = string.Empty;
    public string ActiveTag { get; set; } = string.Empty;
    public string CampaignTitle { get; set; } = string.Empty;
    public string CampaignDescription { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public List<HotelListingCardViewModel> Hotels { get; set; } = new();
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public List<string> Districts { get; set; } = new();
    public List<int> StarOptions { get; set; } = new();
    public List<HotelListingQuickLinkViewModel> QuickLinks { get; set; } = new();
}

public class HotelListingQuickLinkViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-circle";
    public string Url { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class HotelListingCardViewModel
{
    public long Id { get; set; }
    public string HotelCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public string RatingText { get; set; } = string.Empty;
    public int ReviewCount { get; set; }
    public decimal? StartingPrice { get; set; }
    public string PriceNote { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public bool IsFavorite { get; set; }
    public List<string> Amenities { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

public class HotelDetailPageViewModel
{
    public long Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string HotelCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string LongDescription { get; set; } = string.Empty;
    public string LocationDescription { get; set; } = string.Empty;
    public byte? StarCount { get; set; }
    public decimal Rating { get; set; }
    public string RatingText { get; set; } = "Yorum Bekleniyor";
    public int ReviewCount { get; set; }
    public TimeSpan? CheckInTime { get; set; }
    public TimeSpan? CheckOutTime { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal LowestRoomPrice { get; set; }
    public string MainImageUrl { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
    public List<string> GalleryImages { get; set; } = new();
    public List<HotelAmenityViewModel> Amenities { get; set; } = new();
    public List<HotelRoomViewModel> Rooms { get; set; } = new();
    public List<HotelReviewViewModel> Reviews { get; set; } = new();
    public List<HotelSimilarCardViewModel> SimilarHotels { get; set; } = new();
    public HotelWeatherWidgetViewModel? Weather { get; set; }
}

public class HotelAmenityViewModel
{
    public string Name { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-circle-check";
}

public class HotelRoomViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Specs { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string CancellationText { get; set; } = "Ucretsiz iptal";
}

public class HotelReviewViewModel
{
    public string Avatar { get; set; } = "OT";
    public string Name { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class HotelSimilarCardViewModel
{
    public string Name { get; set; } = string.Empty;
    public string PriceText { get; set; } = string.Empty;
    public string RatingText { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}

