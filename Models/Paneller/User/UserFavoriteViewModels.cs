namespace otelturizmnew.Models.Paneller.User;

public class UserFavoritesPageViewModel
{
    public int FavoriteCount { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 7;
    public int TotalPages { get; set; } = 1;
    public string SearchTerm { get; set; } = string.Empty;
    public string Sort { get; set; } = "latest-reservation";
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
    public List<UserFavoriteHotelCardViewModel> Hotels { get; set; } = new();
}

public class UserFavoriteHotelCardViewModel
{
    public long HotelId { get; set; }
    public string HotelCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
    public decimal? StartingPrice { get; set; }
    public decimal? NightlyPrice { get; set; }
    public decimal? DiscountedNightlyPrice { get; set; }
    public int DiscountPercent { get; set; }
    public string PriceText { get; set; } = string.Empty;
    public string NightlyPriceText { get; set; } = string.Empty;
    public string? DiscountedNightlyPriceText { get; set; }
    public string? DiscountPercentText { get; set; }
    public string? PriceDateText { get; set; }
    public bool HasNightlyDiscount => DiscountedNightlyPrice.HasValue && DiscountPercent > 0;
    public string RatingText { get; set; } = string.Empty;
    public DateTime FavoriteAddedAt { get; set; }
    public string AddedDateText { get; set; } = string.Empty;
    public DateTime? LastReservationDate { get; set; }
    public string LastReservationDateText { get; set; } = string.Empty;
    public string? AvailabilityNote { get; set; }
    public int PastStayCount { get; set; }
    public int ReservationCount { get; set; }
    public int ReviewGivenCount { get; set; }
    public int ReviewPendingCount { get; set; }
    /// <summary>Yorum yazılabilir ilk konaklama rezervasyonu (panel yorum formu).</summary>
    public long? FirstEligibleReviewReservationId { get; set; }
    public decimal UserAverageRating { get; set; }
    public string ReservationCountText { get; set; } = string.Empty;
    public string ReviewGivenText { get; set; } = string.Empty;
    public string ReviewPendingText { get; set; } = string.Empty;
    public string UserAverageRatingText { get; set; } = string.Empty;
    public bool PriceAlertEnabled { get; set; }
    public string? PriceAlertTargetText { get; set; }
    public string? PriceAlertDateRangeText { get; set; }
    public string? PriceAlertLastTriggeredText { get; set; }
    public decimal? PriceAlertTargetAmount { get; set; }
    public string? PriceAlertStartDateValue { get; set; }
    public string? PriceAlertEndDateValue { get; set; }
}

public class UserFavoritePriceAlertForm
{
    public long HotelId { get; set; }
    public bool Enabled { get; set; }
    public string? TargetPriceText { get; set; }
    public string? StartDateText { get; set; }
    public string? EndDateText { get; set; }
}
