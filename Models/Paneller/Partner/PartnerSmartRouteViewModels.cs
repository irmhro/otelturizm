namespace otelturizmnew.Models.Paneller.Partner;

public class PartnerSmartRoutesPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public bool TablesReady { get; set; } = true;
    public List<PartnerSmartRouteTagViewModel> Tags { get; set; } = new();
}

public class PartnerSmartRouteTagViewModel
{
    public long Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Hashtag { get; set; } = string.Empty;
    public string SearchText { get; set; } = string.Empty;
    public string ColorClass { get; set; } = "sage";
    public bool IsJoined { get; set; }
    public DateTime? JoinDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class PartnerSmartRouteToggleRequest
{
    public long HotelId { get; set; }
    public long SmartRouteId { get; set; }
    public bool Join { get; set; }
}
