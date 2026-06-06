namespace otelturizmnew.Models.Paneller.Admin;

public class AdminShellViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserRole { get; set; } = "admin";
    public HashSet<string> Permissions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string PanelTitle { get; set; } = string.Empty;
    public string PanelSubtitle { get; set; } = string.Empty;
    public int PendingPartnerApplications { get; set; }
    public int UnreadNotifications { get; set; }
    public int CriticalLogs { get; set; }
    public int PendingReviews { get; set; }
    public int PendingCompanyApplications { get; set; }

    public bool HasPermission(string code) => Permissions.Contains(code);
}

public class AdminDashboardViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public int PendingApprovalsQueueCount { get; set; }
    public List<AdminMetricCardViewModel> Metrics { get; set; } = new();
    public List<AdminMetricCardViewModel> Revenue30DayMetrics { get; set; } = new();
    public List<AdminChartBarViewModel> ReservationChart { get; set; } = new();
    public List<AdminActivityViewModel> Activities { get; set; } = new();
    public List<AdminDashboardHotelRowViewModel> HighlightHotels { get; set; } = new();
    public List<AdminDashboardHotelKpiRowViewModel> HotelKpis { get; set; } = new();
}

public class AdminSectionPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public string SectionKey { get; set; } = string.Empty;
    public List<AdminSummaryCardViewModel> SummaryCards { get; set; } = new();
    public List<AdminTableColumnViewModel> Columns { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
    public string EmptyStateMessage { get; set; } = "Bu sayfa için gösterilecek veri bulunamadı.";
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

public class AdminDashboardHotelKpiRowViewModel
{
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string CityLabel { get; set; } = string.Empty;
    public string PublishStatus { get; set; } = string.Empty;
    public int ReservationCount { get; set; }
    public int CancelledCount { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal CommissionAmount { get; set; }
    public int ReviewCount { get; set; }
    public decimal AverageScore { get; set; }
    public decimal CancelRatePercent => ReservationCount <= 0 ? 0m : Math.Round(CancelledCount * 100m / ReservationCount, 1, MidpointRounding.AwayFromZero);
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
    public bool EmailLoginApproved { get; set; }
    public int DocumentCount { get; set; }
    public string? ReviewNote { get; set; }
}

public class AdminPartnerEmailLoginApprovalRequest
{
    public long PartnerId { get; set; }
    public bool Approved { get; set; } = true;
    public string? Note { get; set; }
}

public sealed class AdminPartnerDocumentsPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminSummaryCardViewModel> SummaryCards { get; set; } = new();
    public string StatusFilter { get; set; } = string.Empty;
    public List<AdminPartnerDocumentQueueRowViewModel> Queue { get; set; } = new();
}

public sealed class AdminPartnerDocumentQueueRowViewModel
{
    public long PartnerId { get; set; }
    public long? HotelId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string PartnerStatus { get; set; } = string.Empty;
    public string PartnerStatusTone { get; set; } = "info";
    public int RequiredDocumentCount { get; set; }
    public int UploadedDocumentCount { get; set; }
    public int PendingReviewCount { get; set; }
    public int ApprovedDocumentCount { get; set; }
    public List<AdminPartnerDocumentChecklistItemViewModel> Checklist { get; set; } = new();
    public List<AdminPartnerDocumentItemViewModel> Documents { get; set; } = new();
}

public sealed class AdminPartnerDocumentChecklistItemViewModel
{
    public string DocumentType { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = true;
    public string StatusText { get; set; } = "Eksik";
    public string ToneClass { get; set; } = "danger";
}

public sealed class AdminPartnerDocumentItemViewModel
{
    public long DocumentId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string StatusToneClass { get; set; } = "info";
    public string UploadedAtText { get; set; } = string.Empty;
    public string? ReviewNote { get; set; }
    public string? AccessUrl { get; set; }
}

public sealed class AdminPartnerDocumentReviewRequest
{
    public long DocumentId { get; set; }
    public long PartnerId { get; set; }
    public string TargetStatus { get; set; } = "Onaylandi";
    public string? Note { get; set; }
}

public class AdminPartnerApplicationDecisionRequest
{
    public long PartnerId { get; set; }
    public string TargetStatus { get; set; } = "Onaylandi";
    public string? Note { get; set; }
    public decimal? CommissionRate { get; set; }
    public List<string>? MissingDocumentTypes { get; set; }
}

public sealed class AdminPartnerApplicationDetailPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public AdminPartnerApplicationDetailViewModel Application { get; set; } = new();
}

public sealed class AdminPartnerApplicationDetailViewModel
{
    public long PartnerId { get; set; }
    public long UserId { get; set; }
    public long? HotelId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyType { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactTitle { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string TaxOffice { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public string ContactTcNo { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string? BankBranch { get; set; }
    public string Iban { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? Description { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string StatusToneClass { get; set; } = "warning";
    public string RegistrationDateText { get; set; } = string.Empty;
    public string? ApprovalDateText { get; set; }
    public bool EmailVerified { get; set; }
    public bool EmailLoginApproved { get; set; }
    public string? ReviewNote { get; set; }
    public decimal CurrentCommissionRate { get; set; }
    public string? HotelApprovalStatus { get; set; }
    public string? HotelPublishStatus { get; set; }
    public List<string> AvailableDocumentTypes { get; set; } = new();
    public List<string> ActiveMissingDocumentTypes { get; set; } = new();
    public List<AdminPartnerDocumentItemViewModel> Documents { get; set; } = new();
    public List<AdminPartnerDocumentChecklistItemViewModel> Checklist { get; set; } = new();
}

public class AdminCompanyApplicationRowViewModel
{
    public long CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string TaxNo { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string StatusToneClass { get; set; } = "warning";
    public string CreatedAtText { get; set; } = string.Empty;
}

public class AdminCompanyApplicationsPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminSummaryCardViewModel> SummaryCards { get; set; } = new();
    public List<AdminCompanyApplicationRowViewModel> Applications { get; set; } = new();
}

public class AdminCompanyApplicationDecisionRequest
{
    public long CompanyId { get; set; }
    public string TargetStatus { get; set; } = "Onaylandı"; // Beklemede/Onaylandı/Reddedildi/Askıda
    public string? Note { get; set; }
}

public sealed class AdminApprovalCenterPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminSummaryCardViewModel> SummaryCards { get; set; } = new();
    public List<AdminApprovalTaskRowViewModel> PendingApprovals { get; set; } = new();
    public List<AdminApprovalHotelRowViewModel> Hotels { get; set; } = new();
    public List<AdminApprovalInvoiceRowViewModel> Invoices { get; set; } = new();
}

public sealed class AdminApprovalTaskRowViewModel
{
    public string Type { get; set; } = string.Empty;
    public long EntityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string ToneClass { get; set; } = "warning";
    public string CreatedAtText { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
}

public sealed class AdminApprovalHotelRowViewModel
{
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string PartnerName { get; set; } = string.Empty;
    public string CityLabel { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public string PublishStatus { get; set; } = string.Empty;
    public string ToneClass { get; set; } = "warning";
    public decimal CommissionRate { get; set; }
    public decimal MonthRevenue { get; set; }
    public decimal MonthCommission { get; set; }
}

public sealed class AdminApprovalInvoiceRowViewModel
{
    public long InvoiceId { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public string InvoiceType { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string BuyerTitle { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string ToneClass { get; set; } = "info";
    public decimal TotalAmount { get; set; }
    public string DateText { get; set; } = string.Empty;
}

public class AdminCommissionRuleRowViewModel
{
    public long RuleId { get; set; }
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string HotelCode { get; set; } = string.Empty;
    public string CityLabel { get; set; } = string.Empty;
    public string DateRangeText { get; set; } = string.Empty;
    public string CommissionText { get; set; } = string.Empty;
    public string TaxText { get; set; } = string.Empty;
    public string NetText { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Note { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionIncomeTaxRate { get; set; }
    public decimal VatRate { get; set; }
    public decimal AccommodationTaxRate { get; set; }
}

public class AdminCommissionManagementPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminSummaryCardViewModel> SummaryCards { get; set; } = new();
    public List<AdminCommissionRuleRowViewModel> Rules { get; set; } = new();
    public List<AdminHotelCommissionFinanceRowViewModel> HotelFinanceRows { get; set; } = new();
    public List<AdminCommissionReservationRowViewModel> ReservationRows { get; set; } = new();
    public AdminCommissionRuleForm Form { get; set; } = new();
    public List<AdminCommissionHotelOptionViewModel> Hotels { get; set; } = new();
    public List<string> CityOptions { get; set; } = new();
    public List<string> DistrictOptions { get; set; } = new();
    public List<string> NeighborhoodOptions { get; set; } = new();
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string Month { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public int PageSize { get; set; } = 50;
    public long? SelectedRuleId { get; set; }
}

public class AdminHotelCommissionFinanceRowViewModel
{
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string HotelCode { get; set; } = string.Empty;
    public string CityLabel { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public decimal GrossRevenue { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal PaidCommission { get; set; }
    public decimal PendingCommission => Math.Max(0m, TotalCommission - PaidCommission);
    public int ReservationCount { get; set; }
    public int CompletedReservationCount { get; set; }
    public int SoldRoomCount { get; set; }
    public decimal PlatformNetCommission { get; set; }
    public string PaymentDueText { get; set; } = string.Empty;
    public string PaymentStatusText { get; set; } = string.Empty;
}

public class AdminCommissionReservationRowViewModel
{
    public long ReservationId { get; set; }
    public string ReservationNo { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string GuestEmail { get; set; } = string.Empty;
    public string StayText { get; set; } = string.Empty;
    public int RoomCount { get; set; }
    public decimal ReservationTotal { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetPayout { get; set; }
    public string PaymentStatusText { get; set; } = string.Empty;
}

public class AdminCommissionHotelOptionViewModel
{
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string HotelCode { get; set; } = string.Empty;
    public string CityLabel { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}

public class AdminCommissionRuleForm
{
    public long? RuleId { get; set; }
    public long HotelId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime? EndDate { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal CommissionIncomeTaxRate { get; set; }
    public decimal VatRate { get; set; }
    public decimal AccommodationTaxRate { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Note { get; set; }
}

public class AdminCommissionCollectionFilter
{
    public string Donem { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public long? IlceId { get; set; }
    public long? HotelId { get; set; }
    public long? PartnerId { get; set; }
    public string TahsilatStatus { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string SortBy { get; set; } = "commission";
    public string SortDir { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class AdminCommissionCollectionRowViewModel
{
    public long HotelId { get; set; }
    public string HotelCode { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public long? IlceId { get; set; }
    public long PartnerId { get; set; }
    public string Donem { get; set; } = string.Empty;
    public int ReservationCount { get; set; }
    public decimal GrossCommission { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public string TahsilatStatus { get; set; } = string.Empty;
    public string PaymentStatusSummary { get; set; } = string.Empty;
}

public class AdminCommissionCollectionTotalsViewModel
{
    public int ReservationTotal { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal CollectedTotal { get; set; }
    public decimal PendingTotal { get; set; }
}

public class AdminCommissionCollectionPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public AdminCommissionCollectionFilter Filter { get; set; } = new();
    public List<AdminCommissionCollectionRowViewModel> Rows { get; set; } = new();
    public AdminCommissionCollectionTotalsViewModel Totals { get; set; } = new();
    public int Total { get; set; }
    public int TotalPages => Filter.PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)Filter.PageSize);
}

public class AdminCommissionCollectionMarkPaidForm
{
    public List<long> HotelIds { get; set; } = new();
    public List<string> Donems { get; set; } = new();
    public string? Reference { get; set; }
    public string? Note { get; set; }
}

public class AdminPlatformTeamPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminPlatformTeamRowViewModel> Members { get; set; } = new();
    public AdminPlatformTeamForm Form { get; set; } = new();
}

public class AdminPlatformTeamRowViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AvatarUrl { get; set; } = string.Empty;
    public int OrderNo { get; set; }
    public bool IsActive { get; set; }
}

public class AdminPlatformTeamForm
{
    public long? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderNo { get; set; }
    public bool IsActive { get; set; } = true;
}

public class AdminHelpCenterPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminHelpCenterCategoryRowViewModel> Categories { get; set; } = new();
    public List<AdminHelpCenterFaqRowViewModel> FaqItems { get; set; } = new();
    public List<AdminHelpCenterContentRowViewModel> Contents { get; set; } = new();

    public AdminHelpCenterCategoryForm CategoryForm { get; set; } = new();
    public AdminHelpCenterFaqForm FaqForm { get; set; } = new();
    public AdminHelpCenterContentForm ContentForm { get; set; } = new();
}

public class AdminHelpCenterCategoryRowViewModel
{
    public long CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string IconClass { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string? HeroTitle { get; set; }
    public string? HeroSubtitle { get; set; }
    public string? HeroImageUrl { get; set; }
    public string? FullHtml { get; set; }
}

public class AdminHelpCenterFaqRowViewModel
{
    public long Id { get; set; }
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int OrderNo { get; set; }
    public bool IsActive { get; set; }
    public string Question { get; set; } = string.Empty;
    public string AnswerHtml { get; set; } = string.Empty;
}

public class AdminHelpCenterContentRowViewModel
{
    public long Id { get; set; }
    public string Type { get; set; } = "blog";
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int OrderNo { get; set; }
    public bool IsFeatured { get; set; }
}

public class AdminHelpCenterCategoryForm
{
    public long CategoryId { get; set; }
    public string? HeroTitle { get; set; }
    public string? HeroSubtitle { get; set; }
    public string? HeroImageUrl { get; set; }
    public string? FullHtml { get; set; }
}

public class AdminHelpCenterFaqForm
{
    public long? Id { get; set; }
    public long CategoryId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string AnswerHtml { get; set; } = string.Empty;
    public int OrderNo { get; set; }
    public bool IsActive { get; set; } = true;
}

public class AdminHelpCenterContentForm
{
    public long? Id { get; set; }
    public string Type { get; set; } = "blog";
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? HeroTitle { get; set; }
    public string? HeroSubtitle { get; set; }
    public string? HeroImageUrl { get; set; }
    public string Html { get; set; } = string.Empty;
    public int OrderNo { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
}

