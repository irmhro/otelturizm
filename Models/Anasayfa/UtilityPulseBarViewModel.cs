namespace otelturizmnew.Models.Anasayfa;

public sealed record UtilityPulseBarViewModel
{
    public int ActiveHotelCount { get; init; }

    public int ActiveCampaignCount { get; init; }

    public IReadOnlyList<UtilityPulseDestinationViewModel> TopDestinations { get; init; } = Array.Empty<UtilityPulseDestinationViewModel>();

    public IReadOnlyList<UtilityPulseInsightViewModel> Insights { get; init; } = Array.Empty<UtilityPulseInsightViewModel>();

    public string Greeting { get; init; } = string.Empty;

    public string LocalTimeLabel { get; init; } = string.Empty;
}

public sealed record UtilityPulseDestinationViewModel
{
    public string City { get; init; } = string.Empty;

    public int HotelCount { get; init; }

    public string ListingUrl { get; init; } = string.Empty;
}

public sealed record UtilityPulseInsightViewModel
{
    public string IconClass { get; init; } = "fas fa-circle-check";

    public string Text { get; init; } = string.Empty;

    public string? LinkUrl { get; init; }
}
