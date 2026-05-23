namespace otelturizmnew.Models.Subscriptions;

public sealed class HotelListingSubscriptionRequest
{
    public long HotelId { get; set; }
    /// <summary>ILLER / ILCELER / MAHALLELER kapsamı: IL, ILCE, MAHALLE.</summary>
    public string ScopeType { get; set; } = "IL";
    public string ScopeValue { get; set; } = string.Empty;
    public int DesiredRank { get; set; } = 1; // 1..3
    public int DayCount { get; set; } = 7; // 1..30
    public string? PartnerNote { get; set; }
}

public sealed class HotelListingSubscriptionRow
{
    public long Id { get; set; }
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string ScopeType { get; set; } = string.Empty;
    public string ScopeValue { get; set; } = string.Empty;
    public int DesiredRank { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PartnerEmail { get; set; } = string.Empty;
    public string PartnerNote { get; set; } = string.Empty;
    public string AdminNote { get; set; } = string.Empty;
}

public sealed class AdminSubscriptionDecisionRequest
{
    public long SubscriptionId { get; set; }
    public string Action { get; set; } = "approve"; // approve / reject / suspend / cancel
    public string? AdminNote { get; set; }
}

