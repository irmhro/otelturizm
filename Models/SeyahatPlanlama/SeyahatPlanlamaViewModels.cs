using otelturizmnew.Models.Kampanyalar;

namespace otelturizmnew.Models.SeyahatPlanlama;

public class SeyahatPlanlamaPageViewModel
{
    public string PageTitle { get; set; } = "Seyahat Planlama";
    public string MetaDescription { get; set; } = string.Empty;
    public bool IsAuthenticated { get; set; }
    public string LoyaltyPanelUrl { get; set; } = "/panel/user/otelpuan-programi";
    public List<TravelDestinationCardViewModel> RouteSuggestions { get; set; } = new();
    public List<TravelWeekendCardViewModel> WeekendEscapes { get; set; } = new();
    public List<CampaignCardViewModel> CampaignSuggestions { get; set; } = new();
    public TravelBudgetFormViewModel BudgetForm { get; set; } = new();
    public TravelBudgetEstimateViewModel? BudgetEstimate { get; set; }
}

public class TravelDestinationCardViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string SearchQuery { get; set; } = string.Empty;
    public string HotelsUrl { get; set; } = string.Empty;
    public string ThemeClass { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fas fa-map-marker-alt";
    public int TypicalNights { get; set; } = 2;
    public decimal MinNightlyTry { get; set; }
}

public class TravelWeekendCardViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string BadgeText { get; set; } = string.Empty;
    public string ThemeClass { get; set; } = string.Empty;
}

public class TravelBudgetFormViewModel
{
    public string DestinationKey { get; set; } = "istanbul";
    public int Nights { get; set; } = 2;
    public decimal BudgetTry { get; set; } = 6000m;
    public bool Submitted { get; set; }
    public string SearchUrl { get; set; } = "/hotel";
}

public class TravelBudgetEstimateViewModel
{
    public string DestinationLabel { get; set; } = string.Empty;
    public int Nights { get; set; }
    public decimal BudgetTry { get; set; }
    public decimal EstimatedMinTotalTry { get; set; }
    public decimal EstimatedMaxTotalTry { get; set; }
    public decimal MinNightlyTry { get; set; }
    public bool FitsBudget { get; set; }
    public string SummaryText { get; set; } = string.Empty;
    public string HotelsUrl { get; set; } = string.Empty;
}
