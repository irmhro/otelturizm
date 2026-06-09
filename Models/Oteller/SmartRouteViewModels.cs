namespace otelturizmnew.Models.Oteller;

public class SmartRouteFilterViewModel
{
    public long Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Hashtag { get; set; } = string.Empty;
    public string SearchText { get; set; } = string.Empty;
    public string ColorClass { get; set; } = "sage";
    public int HotelCount { get; set; }
}
