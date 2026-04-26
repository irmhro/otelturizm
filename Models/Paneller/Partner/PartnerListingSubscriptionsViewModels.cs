namespace otelturizmnew.Models.Paneller.Partner;

public sealed class PartnerListingSubscriptionsPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public long SelectedHotelId { get; set; }
    public string SelectedHotelName { get; set; } = string.Empty;

    public PartnerListingSubscriptionCreateRequest Form { get; set; } = new();
    public List<PartnerListingSubscriptionRowViewModel> Items { get; set; } = new();
}

public sealed class PartnerListingSubscriptionCreateRequest
{
    public long HotelId { get; set; }
    public string ScopeType { get; set; } = "IL"; // IL / ILCE / MAHALLE
    public string ScopeValue { get; set; } = string.Empty;
    public int DesiredRank { get; set; } = 1; // 1..3
    public int DayCount { get; set; } = 7; // 1..30
    public string? PartnerNote { get; set; }
}

public sealed class PartnerListingSubscriptionRowViewModel
{
    public long Id { get; set; }
    public string ScopeType { get; set; } = string.Empty;
    public string ScopeValue { get; set; } = string.Empty;
    public int DesiredRank { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StartText { get; set; } = "-";
    public string EndText { get; set; } = "-";
    public string Note { get; set; } = string.Empty;
}

