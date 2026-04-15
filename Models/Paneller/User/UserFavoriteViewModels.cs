namespace otelturizmnew.Models.Paneller.User;

public class UserFavoritesPageViewModel
{
    public int FavoriteCount { get; set; }
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
    public string PriceText { get; set; } = string.Empty;
    public string RatingText { get; set; } = string.Empty;
    public string AddedDateText { get; set; } = string.Empty;
    public int PastStayCount { get; set; }
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
