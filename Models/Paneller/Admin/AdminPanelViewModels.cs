namespace otelturizmnew.Models.Paneller.Admin;

public class AdminShellViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserRole { get; set; } = "admin";
    public string PanelTitle { get; set; } = string.Empty;
    public string PanelSubtitle { get; set; } = string.Empty;
    public int PendingPartnerApplications { get; set; }
    public int UnreadNotifications { get; set; }
    public int CriticalLogs { get; set; }
    public int PendingReviews { get; set; }
}

public class AdminDashboardViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminMetricCardViewModel> Metrics { get; set; } = new();
    public List<AdminChartBarViewModel> ReservationChart { get; set; } = new();
    public List<AdminActivityViewModel> Activities { get; set; } = new();
    public List<AdminDashboardHotelRowViewModel> HighlightHotels { get; set; } = new();
}

public class AdminSectionPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public string SectionKey { get; set; } = string.Empty;
    public List<AdminSummaryCardViewModel> SummaryCards { get; set; } = new();
    public List<AdminTableColumnViewModel> Columns { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
    public string EmptyStateMessage { get; set; } = "Bu sayfa icin gosterilecek veri bulunamadi.";
    public string? InfoNote { get; set; }
}

public class AdminMetricCardViewModel
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string TrendText { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-chart-line";
    public string ToneClass { get; set; } = "info";
}

public class AdminChartBarViewModel
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
    public int HeightPercent { get; set; }
}

public class AdminActivityViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-bolt";
    public string ToneClass { get; set; } = "info";
    public string TimeText { get; set; } = string.Empty;
}

public class AdminDashboardHotelRowViewModel
{
    public string HotelName { get; set; } = string.Empty;
    public string CityLabel { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusToneClass { get; set; } = "info";
    public string ScoreText { get; set; } = "-";
    public string ReservationText { get; set; } = "0";
}

public class AdminSummaryCardViewModel
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ToneClass { get; set; } = "info";
    public string IconClass { get; set; } = "fa-layer-group";
}

public class AdminTableColumnViewModel
{
    public string Label { get; set; } = string.Empty;
}

public class AdminPartnerApplicationsPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminSummaryCardViewModel> SummaryCards { get; set; } = new();
    public List<AdminPartnerApplicationRowViewModel> Applications { get; set; } = new();
}

public class AdminPartnerApplicationRowViewModel
{
    public long PartnerId { get; set; }
    public long UserId { get; set; }
    public long? HotelId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string StatusToneClass { get; set; } = "warning";
    public string RegistrationDateText { get; set; } = string.Empty;
    public string? ApprovalDateText { get; set; }
    public bool EmailVerified { get; set; }
    public int DocumentCount { get; set; }
    public string? ReviewNote { get; set; }
}

public class AdminPartnerApplicationDecisionRequest
{
    public long PartnerId { get; set; }
    public string TargetStatus { get; set; } = "Onaylandi";
    public string? Note { get; set; }
}

