namespace otelturizmnew.Models.Paneller.Admin;

public sealed class AdminListingSubscriptionsPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminSummaryCardViewModel> SummaryCards { get; set; } = new();
    public List<AdminListingSubscriptionRowViewModel> Rows { get; set; } = new();
}

public sealed class AdminListingSubscriptionRowViewModel
{
    public long SubscriptionId { get; set; }
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string CityText { get; set; } = string.Empty;
    public string ScopeType { get; set; } = string.Empty;
    public string ScopeValue { get; set; } = string.Empty;
    public int Rank { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string StatusToneClass { get; set; } = "info";
    public string StartText { get; set; } = "-";
    public string EndText { get; set; } = "-";
    public string PartnerEmail { get; set; } = string.Empty;
    public string PartnerNote { get; set; } = string.Empty;
}

public sealed class AdminListingSubscriptionDecisionRequest
{
    public long SubscriptionId { get; set; }
    public string Action { get; set; } = "approve"; // approve / reject / suspend / cancel
    public string? AdminNote { get; set; }
}

