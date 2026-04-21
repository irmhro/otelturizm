using otelturizmnew.Models.Reservations;

namespace otelturizmnew.Models.Oteller;

public class HotelListingPageViewModel
{
    public string City { get; set; } = string.Empty;
    public string SearchTerm { get; set; } = string.Empty;
    public string SearchLabel { get; set; } = string.Empty;
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

public class HotelSearchSuggestionViewModel
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
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
    public string PropertyType { get; set; } = string.Empty;
    public int? StarCount { get; set; }
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
    public bool IsLoggedInUser { get; set; }
    public bool HasCompletedReservationAtHotel { get; set; }
    public string ConversationInfoMessage { get; set; } = string.Empty;
    public bool ShouldResumeDraftOnLoad { get; set; }
    public PublicHotelReservationForm ReservationForm { get; set; } = new();
    public ReservationDraftSummaryViewModel? ActiveDraft { get; set; }
    public HotelProfileCompletionPromptViewModel ProfilePrompt { get; set; } = new();
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
    public long RoomTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Specs { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public byte MaxGuestCount { get; set; }
    public byte MaxAdultCount { get; set; }
    public byte MaxChildCount { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> GalleryImages { get; set; } = new();
    public List<HotelRoomFeatureViewModel> Features { get; set; } = new();
    public string CancellationText { get; set; } = "Ucretsiz iptal";
}

public class HotelRoomFeatureViewModel
{
    public string Name { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-circle-check";
}

public class HotelProfileCompletionPromptViewModel
{
    public bool IsProfileIncomplete { get; set; }
    public string ReturnUrl { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? BirthDateText { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Neighborhood { get; set; }
    public string? Address { get; set; }
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
