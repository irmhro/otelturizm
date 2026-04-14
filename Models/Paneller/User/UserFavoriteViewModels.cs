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
}
