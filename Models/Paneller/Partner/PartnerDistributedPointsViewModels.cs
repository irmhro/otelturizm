namespace otelturizmnew.Models.Paneller.Partner;

public class PartnerDistributedPointsPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public long? SelectedHotelId { get; set; }
    public int TotalDistributedPoints { get; set; }
    public int ActiveBalanceCount { get; set; }
    public List<PartnerDistributedPointsRowViewModel> Rows { get; set; } = new();
}

public class PartnerDistributedPointsRowViewModel
{
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public long UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public int TotalEarned { get; set; }
    public int AvailablePoints { get; set; }
    public int UsedPoints { get; set; }
    public string? LastEarnedText { get; set; }
}
