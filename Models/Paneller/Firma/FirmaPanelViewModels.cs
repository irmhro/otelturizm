using otelturizmnew.Models.Messages;
using otelturizmnew.Models.TelefonDogrulama;

namespace otelturizmnew.Models.Paneller.Firma;

public class FirmaPanelShellViewModel
{
    public long UserId { get; set; }
    public long FirmaId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public string CompanyBadgeText { get; set; } = "Aktif Firma";
    public string PanelTitle { get; set; } = string.Empty;
    public string PanelSubtitle { get; set; } = string.Empty;
    public string ActiveSectionKey { get; set; } = "dashboard";
    public int PendingApprovalCount { get; set; }
    public int EmployeeCount { get; set; }
    public int DealCount { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}

public class FirmaPanelStatCardViewModel
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-chart-line";
    public string ToneClass { get; set; } = "info";
}

public class FirmaPanelReservationRowViewModel
{
    public long ReservationId { get; set; }
    public string ReservationNo { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string StayText { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string ApprovalText { get; set; } = string.Empty;
    public string TotalText { get; set; } = string.Empty;
    public bool CanApprove { get; set; }
    public string HotelCityText { get; set; } = string.Empty;
}

public class FirmaPanelDealRowViewModel
{
    public long DealId { get; set; }
    public long HotelId { get; set; }
    public long RoomTypeId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string CityText { get; set; } = string.Empty;
    public string CapacityText { get; set; } = "-";
    public string StockText { get; set; } = "-";
    public string StandardPriceText { get; set; } = string.Empty;
    public string CorporatePriceText { get; set; } = string.Empty;
    public string DiscountText { get; set; } = string.Empty;
    public string MinimumRoomText { get; set; } = string.Empty;
    public string ValidityText { get; set; } = string.Empty;
    public string SavingsText { get; set; } = string.Empty;
}

public class FirmaPanelEmployeeRowViewModel
{
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string LimitText { get; set; } = string.Empty;
    public string ApprovalText { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string ReservationCountText { get; set; } = "0";
    public string SpendText { get; set; } = "₺0";
    public string RoleText { get; set; } = string.Empty;
    public string RoleCode { get; set; } = "firma_staff";
    public bool IsManager { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsPhoneVerified { get; set; }
    public string PhoneVerificationText { get; set; } = "Telefon doğrulaması bekleniyor";
    public string PhoneVerificationToneClass { get; set; } = "is-warning";
    public decimal? NightlyLimit { get; set; }
    public bool ApprovalRequired { get; set; }
}

public class FirmaPanelLimitRowViewModel
{
    public long LimitId { get; set; }
    public long? UserId { get; set; }
    public string? Department { get; set; }
    public string ScopeText { get; set; } = string.Empty;
    public string NightlyLimitText { get; set; } = string.Empty;
    public string ReservationLimitText { get; set; } = string.Empty;
    public string MonthlyLimitText { get; set; } = string.Empty;
    public string ApprovalText { get; set; } = string.Empty;
    public bool ApprovalRequired { get; set; }
}

public class FirmaPanelInvoiceRowViewModel
{
    public long InvoiceId { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public string InvoiceDateText { get; set; } = string.Empty;
    public string InvoiceType { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string TotalText { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public bool CanDownload { get; set; }
}

public class FirmaMonthlySpendRowViewModel
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int ReservationCount { get; set; }
    public int HeightPercent { get; set; }
}

public class FirmaHotelReportRowViewModel
{
    public string HotelName { get; set; } = string.Empty;
    public string CityText { get; set; } = string.Empty;
    public int ReservationCount { get; set; }
    public string GrossAmountText { get; set; } = string.Empty;
    public string SavingsText { get; set; } = string.Empty;
}

public class FirmaDashboardPageViewModel
{
    public FirmaPanelShellViewModel Shell { get; set; } = new();
    public UserPhoneVerificationStatusViewModel PhoneVerification { get; set; } = new();
    public List<FirmaPanelStatCardViewModel> SummaryCards { get; set; } = new();
    public List<FirmaPanelDealRowViewModel> HighlightDeals { get; set; } = new();
    public List<FirmaPanelEmployeeRowViewModel> FeaturedEmployees { get; set; } = new();
    public List<FirmaPanelReservationRowViewModel> RecentReservations { get; set; } = new();

    /// <summary>Bu ay etiketi (örn. Mayıs 2026).</summary>
    public string CurrentMonthLabel { get; set; } = string.Empty;

    public string MonthSpendTotalText { get; set; } = "₺0";
    public string MonthSavingsText { get; set; } = "₺0";
    public int MonthReservationCount { get; set; }

    /// <summary>Son aylar için mini grafik (Harcama raporu ile aynı kaynak).</summary>
    public List<FirmaMonthlySpendRowViewModel> SpendTrend { get; set; } = new();

    /// <summary>Firma geneli aylık limit vb. uyarı metinleri.</summary>
    public List<string> LimitAlerts { get; set; } = new();
}

public class FirmaDealsPageViewModel
{
    public FirmaPanelShellViewModel Shell { get; set; } = new();
    public List<FirmaPanelDealRowViewModel> Deals { get; set; } = new();
    public FirmaDealsFilterModel Filter { get; set; } = new();
    public List<string> AvailableCities { get; set; } = new();
    public List<string> AvailableDistricts { get; set; } = new();
    public List<string> AvailableNeighborhoods { get; set; } = new();
    public List<FirmaDealHotelOptionViewModel> HotelOptions { get; set; } = new();
}

public class FirmaDealHotelOptionViewModel
{
    public long HotelId { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}

public class FirmaDealsComparePageViewModel
{
    public FirmaPanelShellViewModel Shell { get; set; } = new();
    public int RoomCount { get; set; } = 5;
    public List<FirmaDealsCompareHotelViewModel> Hotels { get; set; } = new();
    public List<FirmaDealsCompareRowViewModel> Rows { get; set; } = new();
    public string? Hint { get; set; }
}

public class FirmaDealsCompareHotelViewModel
{
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string CityText { get; set; } = string.Empty;
}

public class FirmaDealsCompareRowViewModel
{
    public long HotelId { get; set; }
    public long RoomTypeId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string CapacityText { get; set; } = "-";
    public string CorporateNightlyText { get; set; } = "-";
    public string StandardNightlyText { get; set; } = "-";
    public string DiscountText { get; set; } = "-";
    public string ValidityText { get; set; } = "-";
}

public class FirmaReservationsPageViewModel
{
    public FirmaPanelShellViewModel Shell { get; set; } = new();
    public List<FirmaPanelReservationRowViewModel> Reservations { get; set; } = new();
    public FirmaReservationFilterViewModel Filters { get; set; } = new();
}

public class FirmaReservationFilterViewModel
{
    public string? Query { get; set; }
    public string Status { get; set; } = "all";
    public string ApprovalStatus { get; set; } = "all";
}

public class FirmaEmployeesPageViewModel
{
    public FirmaPanelShellViewModel Shell { get; set; } = new();
    public List<FirmaPanelEmployeeRowViewModel> Employees { get; set; } = new();
    public FirmaEmployeeCreateModel CreateForm { get; set; } = new();
    public int TravelingEmployeeCount { get; set; }
    public string AverageLimitText { get; set; } = "₺0";
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int TotalPages { get; set; }
    public string? SearchTerm { get; set; }
    public string? DepartmentFilter { get; set; }
    public List<string> Departments { get; set; } = new();
}

public class FirmaLimitsPageViewModel
{
    public FirmaPanelShellViewModel Shell { get; set; } = new();
    public List<FirmaPanelLimitRowViewModel> Limits { get; set; } = new();
    public List<FirmaPanelReservationRowViewModel> PendingApprovals { get; set; } = new();
    public FirmaLimitUpsertModel LimitForm { get; set; } = new();
    public List<FirmaPanelEmployeeRowViewModel> Employees { get; set; } = new();
}

public class FirmaInvoicesPageViewModel
{
    public FirmaPanelShellViewModel Shell { get; set; } = new();
    public List<FirmaPanelInvoiceRowViewModel> Invoices { get; set; } = new();
}

public class FirmaSpendingReportsPageViewModel
{
    public FirmaPanelShellViewModel Shell { get; set; } = new();
    public List<FirmaMonthlySpendRowViewModel> MonthlySpend { get; set; } = new();
    public string TotalSpendText { get; set; } = string.Empty;
}

public class FirmaHotelReportsPageViewModel
{
    public FirmaPanelShellViewModel Shell { get; set; } = new();
    public List<FirmaHotelReportRowViewModel> HotelReports { get; set; } = new();
}

public class FirmaMessagesPageViewModel
{
    public FirmaPanelShellViewModel Shell { get; set; } = new();
    public List<MessageCenterThreadViewModel> Threads { get; set; } = new();
    public long? SelectedConversationId { get; set; }
    public string SelectedTitle { get; set; } = "Firma Mesajları";
    public string SelectedSubtitle { get; set; } = "Kullanıcı yazışmaları";
    public List<MessageCenterItemViewModel> Messages { get; set; } = new();
}

public class FirmaEmployeeCreateModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal? NightlyLimit { get; set; }
    public bool ApprovalRequired { get; set; }
    public string Role { get; set; } = "firma_staff";
}

public class FirmaEmployeeUpdateModel
{
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal? NightlyLimit { get; set; }
    public bool ApprovalRequired { get; set; }
    public string Role { get; set; } = "firma_staff";
    public bool IsActive { get; set; } = true;
    public string? ReturnUrl { get; set; }
}

public class FirmaEmployeeStatusModel
{
    public long UserId { get; set; }
    public bool IsActive { get; set; }
    public string? ReturnUrl { get; set; }
}

public class FirmaLimitUpsertModel
{
    public long? UserId { get; set; }
    public string? Department { get; set; }
    public decimal? NightlyLimit { get; set; }
    public decimal? ReservationLimit { get; set; }
    public decimal? MonthlyLimit { get; set; }
    public bool ApprovalRequired { get; set; }
}

public class FirmaReservationDecisionModel
{
    public long ReservationId { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
}

public class FirmaAccountPageViewModel
{
    public FirmaPanelShellViewModel Shell { get; set; } = new();
    public FirmaAccountUpdateModel UpdateForm { get; set; } = new();
    public string CompanyStatusText { get; set; } = string.Empty;
    public string CompanyTaxNo { get; set; } = string.Empty;
}

public class FirmaAccountUpdateModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string RoleText { get; set; } = string.Empty;
}

public class FirmaDealsFilterModel
{
    public string? Search { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Neighborhood { get; set; }
    public long? IlId { get; set; }
    public long? IlceId { get; set; }
    public long? MahalleId { get; set; }
    public int? MinRoomCount { get; set; }
}
