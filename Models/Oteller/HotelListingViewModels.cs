using otelturizmnew.Models.Reservations;

using otelturizmnew.Pricing;

namespace otelturizmnew.Models.Oteller;

public class HotelListingPageViewModel
{
    public string City { get; set; } = string.Empty;
    public string SearchTerm { get; set; } = string.Empty;
    public string SearchLabel { get; set; } = string.Empty;
    public string ActiveTag { get; set; } = string.Empty;
    public string CampaignSlug { get; set; } = string.Empty;
    public string CampaignTitle { get; set; } = string.Empty;
    public string CampaignDescription { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; }
    public List<HotelListingCardViewModel> Hotels { get; set; } = new();
    public List<HotelListingCardViewModel> MapHotels { get; set; } = new();
    public List<string> Cities { get; set; } = new();
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public decimal? ActiveMinPrice { get; set; }
    public decimal? ActiveMaxPrice { get; set; }
    public List<string> Districts { get; set; } = new();
    public List<string> Neighborhoods { get; set; } = new();
    public List<int> StarOptions { get; set; } = new();
    public List<string> PropertyTypes { get; set; } = new();
    public List<HotelListingCampaignFilterViewModel> Campaigns { get; set; } = new();
    public List<HotelListingQuickLinkViewModel> QuickLinks { get; set; } = new();

    /// <summary>Wave-XIV: liste kartlarında sadakat rozeti veya placeholder göster.</summary>
    public bool ShowLoyaltyTouchpoints { get; set; } = true;

    public bool LoyaltyUserSignedIn { get; set; }
}

public class HotelSearchSuggestionViewModel
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class HotelListingQuickLinkViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-circle";
    public string Url { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class HotelListingCampaignFilterViewModel
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int HotelCount { get; set; }
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
    public string Neighborhood { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal Rating { get; set; }
    public string RatingText { get; set; } = string.Empty;
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
    public string PriceNote { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public List<string> GalleryImages { get; set; } = new();
    public bool IsFeatured { get; set; }
    public bool IsFavorite { get; set; }
    public List<string> Amenities { get; set; } = new();
    public List<HotelAmenityViewModel> AmenityItems { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<string> CampaignNames { get; set; } = new();
    public List<string> CampaignSlugs { get; set; } = new();
    public string CampaignBadgeText { get; set; } = string.Empty;
    public string CampaignInfoText { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;

    /// <summary>Bugün için satışa açık ve stoklu oda var mı?</summary>
    public bool HasAvailabilityToday { get; set; } = true;

    /// <summary>Listede gösterilen gecelik fiyatın bağlı olduğu oda tipi adı (bugünkü satır).</summary>
    public string? ListingLeadRoomName { get; set; }

    /// <summary>Liste kartında ücretsiz iptal rozeti (T381).</summary>
    public bool HasFreeCancellation { get; set; }

    /// <summary>Tahmini gecelik sadakat puanı (giriş yapmış kullanıcı rozeti).</summary>
    public int? EstimatedLoyaltyPoints { get; set; }
}

public class HotelDetailPageViewModel
{
    public long Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string HotelCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public long? UlkeId { get; set; }
    public long? SehirId { get; set; }
    public long? IlceId { get; set; }
    public long? MahalleId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string LongDescription { get; set; } = string.Empty;
    public string LocationDescription { get; set; } = string.Empty;
    public byte? StarCount { get; set; }
    public decimal Rating { get; set; }
    public string RatingText { get; set; } = "Yorum Bekleniyor";
    public int ReviewCount { get; set; }
    /// <summary>Konum ortalamasi (1-10 vitrin).</summary>
    public decimal ReviewLocationScore { get; set; }
    /// <summary>Temizlik ortalamasi (1-10 vitrin).</summary>
    public decimal ReviewCleaningScore { get; set; }
    /// <summary>Oda / konaklama kalitesi ortalamasi (1-10 vitrin, legacy).</summary>
    public decimal ReviewRoomScore { get; set; }
    /// <summary>Konfor ortalamasi; yeni modelde temizlik/oda puanina esler (geriye donuk).</summary>
    public decimal ReviewComfortScore { get; set; }
    /// <summary>Fiyat/performans ortalamasi (1-10 vitrin).</summary>
    public decimal ReviewValueScore { get; set; }
    /// <summary>Personel ortalamasi (1-10 vitrin).</summary>
    public decimal ReviewStaffScore { get; set; }
    /// <summary>Sessizlik ortalamasi (1-10 vitrin).</summary>
    public decimal ReviewQuietnessScore { get; set; }
    /// <summary>Ulasim ortalamasi (1-10 vitrin).</summary>
    public decimal ReviewTransportScore { get; set; }
    public TimeSpan? CheckInTime { get; set; }
    public TimeSpan? CheckOutTime { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal LowestRoomPrice { get; set; }
    /// <summary>Fiyat vitrininde kullanilan KDV orani (komisyon_vergiler).</summary>
    public decimal TaxDisplayVatPercent { get; set; } = 10m;
    /// <summary>Fiyat vitrininde kullanilan konaklama vergisi orani.</summary>
    public decimal TaxDisplayAccommodationPercent { get; set; } = 2m;

    public decimal GuestInclusiveNightlyFromStoredNet(decimal storedNet)
        => InclusiveNightlyPricing.StoredNetToGuestDisplay(storedNet, TaxDisplayVatPercent, TaxDisplayAccommodationPercent);

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

    /// <summary>Kısa TTL ile üretilen tahmini aktif görüntüleyici bandı (growth vitrin).</summary>
    public int ActiveViewerBand { get; set; }

    /// <summary>Canlı oturum sayacı (heartbeat, kısa TTL).</summary>
    public int LivePresenceCount { get; set; }

    /// <summary>?trip= veya çerez ile gelen kullanıcı niyeti etiketi (segmentasyon).</summary>
    public string IntentSegmentLabel { get; set; } = string.Empty;

    /// <summary>Seçili giriş tarihi için satışa açık oda bulunamadı.</summary>
    public bool HasNoOpenRoomsForSelectedDate { get; set; }

    /// <summary>Seçili tarih için oda yoksa kullanıcıya gösterilecek uyarı metni.</summary>
    public string NoOpenRoomsMessage { get; set; } = string.Empty;

    /// <summary>Envanter kontrolünün yapıldığı tarih (genelde ReservationForm.CheckInDate).</summary>
    public DateOnly AvailabilityCheckDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Geliştirme için: bugün/1 hafta kapalı satış raporu (email'e hazır, şimdilik gönderilmez).</summary>
    public string InventoryClosureWeekReport { get; set; } = string.Empty;

    /// <summary>Otele ait tanıtım videosu (YouTube / URL). `oteller.video_url`.</summary>
    public string? VideoUrl { get; set; }

    /// <summary>Otelin dahil olduğu aktif kampanyalar (etiket/isim).</summary>
    public List<string> CampaignNames { get; set; } = new();

    /// <summary>Kampanya detay sayfası için slug listesi (seo_slug veya kampanya_kodu).</summary>
    public List<string> CampaignSlugs { get; set; } = new();

    /// <summary>Giriş yapmış kullanıcı için bu otelde yorum yazılabilir geçmiş konaklamalar.</summary>
    public List<HotelEligibleReviewStayViewModel> EligibleReviewStays { get; set; } = new();

    /// <summary>`OTEL_KOSULLARI` — iptal, sigara, evcil hayvan vb.</summary>
    public HotelDetailConditionsViewModel? Conditions { get; set; }

    public bool HasFreeCancellation => Conditions?.HasFreeCancellation == true;
}

public class HotelEligibleReviewStayViewModel
{
    public long ReservationId { get; set; }
    public string StayDateText { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
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
    public string BedType { get; set; } = string.Empty;
    public ushort? SquareMeter { get; set; }
    public string DetailDescription { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? BasePrice { get; set; }
    public decimal? DiscountPrice { get; set; }
    public long? DiscountId { get; set; }
    public string? DiscountName { get; set; }
    public string? DiscountShortDescription { get; set; }
    public string? DiscountImageUrl { get; set; }
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
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? BirthDateText { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Neighborhood { get; set; }
    public long? UlkeId { get; set; }
    public long? IlId { get; set; }
    public long? IlceId { get; set; }
    public long? MahalleId { get; set; }
    public string? Address { get; set; }
}

public class HotelReviewViewModel
{
    public string Avatar { get; set; } = "OT";
    public string Name { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? TravelProfile { get; set; }
    public string? SatisfactionLabel { get; set; }
    public string? ReservationNoTail { get; set; }
}

public class HotelSimilarCardViewModel
{
    public string Name { get; set; } = string.Empty;
    public string PriceText { get; set; } = string.Empty;
    public string RatingText { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}
