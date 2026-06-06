namespace otelturizmnew.Models.Oteller;

public class HotelDetailConditionsViewModel
{
    public string CancellationSummary { get; set; } = string.Empty;
    public string CancellationDetail { get; set; } = string.Empty;
    public byte? FreeCancellationHours { get; set; }
    public bool HasFreeCancellation => FreeCancellationHours is >= 1;
    public string SmokingPolicy { get; set; } = string.Empty;
    public string PetPolicy { get; set; } = string.Empty;
    public string ChildPolicy { get; set; } = string.Empty;
    public bool PrepaymentRequired { get; set; }
    public decimal PrepaymentPercent { get; set; }
    public bool CardPaymentAccepted { get; set; } = true;
}
