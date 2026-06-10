namespace otelturizmnew.Models.Payments;

public static class PublicSavedPaymentMethods
{
    public const string SavedCard = "Kayıtlı kredi kartımla öde";
    public const int MaxPartnerCardViews = 3;
}

public sealed class PaymentCardPayload
{
    public string Pan { get; set; } = string.Empty;
    public string HolderName { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string Brand { get; set; } = string.Empty;
}

public sealed class SavedPaymentCardOptionViewModel
{
    public long PaymentMethodId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string MaskedPan { get; set; } = string.Empty;
    public string ExpiryText { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public sealed class PartnerPaymentCardViewResult
{
    public bool Success { get; set; }
    public bool LimitExceeded { get; set; }
    public int ViewCount { get; set; }
    public int RemainingViews { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? CardHolder { get; set; }
    public string? CardNumber { get; set; }
    public string? ExpiryText { get; set; }
    public string? Brand { get; set; }
    public string? MaskedPan { get; set; }
}
