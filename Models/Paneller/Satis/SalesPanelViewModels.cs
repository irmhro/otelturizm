namespace otelturizmnew.Models.Paneller.Satis;

public class SalesPanelShellViewModel
{
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public decimal DailyTarget { get; set; }
    public decimal MonthlyTarget { get; set; }
    public string PanelTitle { get; set; } = string.Empty;
    public string PanelSubtitle { get; set; } = string.Empty;
    public string ActiveSectionKey { get; set; } = "dashboard";
    public int TodayReservationCount { get; set; }
    public decimal TodayRevenue { get; set; }
    public int MonthlyReservationCount { get; set; }
    public int Ranking { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SalesReservationListItemViewModel
{
    public long ReservationId { get; set; }
    public string ReservationNo { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string StayText { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string ApprovalText { get; set; } = string.Empty;
    public string TotalText { get; set; } = string.Empty;
    public string CommissionText { get; set; } = string.Empty;
    public string CreatedAtText { get; set; } = string.Empty;
    public string ChannelText { get; set; } = string.Empty;
    public string? DemandNote { get; set; }
}

public class SalesCustomerCardViewModel
{
    public long CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string MembershipLevel { get; set; } = string.Empty;
    public string ReservationCountText { get; set; } = string.Empty;
    public string LastStayText { get; set; } = string.Empty;
    public string LastRequestSummary { get; set; } = string.Empty;
    public string TotalSpendText { get; set; } = string.Empty;
    public string LocationText { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Neighborhood { get; set; }
    public string? Address { get; set; }
}

public class SalesHotelGuideItemViewModel
{
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string SalesContactName { get; set; } = string.Empty;
    public string SalesContactPhone { get; set; } = string.Empty;
    public string SalesContactEmail { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string PreferredCountText { get; set; } = string.Empty;
}

public class SalesAvailabilityDayViewModel
{
    public DateOnly Date { get; set; }
    public bool IsAvailable { get; set; }
    public string PriceText { get; set; } = string.Empty;
    public string CampaignPriceText { get; set; } = string.Empty;
    public string StockText { get; set; } = string.Empty;
    public string SoldOutText { get; set; } = string.Empty;
}

public class SalesStatCardViewModel
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = string.Empty;
    public string ToneClass { get; set; } = string.Empty;
}

public class SalesDashboardPageViewModel
{
    public SalesPanelShellViewModel Shell { get; set; } = new();
    public List<SalesStatCardViewModel> SummaryCards { get; set; } = new();
    public List<SalesReservationListItemViewModel> RecentReservations { get; set; } = new();
    public int MonthlyReservationCount { get; set; }
    public decimal MonthlyAchievedRevenue { get; set; }
    public decimal MonthlyRemainingRevenue { get; set; }
    public int RemainingReservationCount { get; set; }
    public int MonthlyProgressPercent { get; set; }
}

public class SalesCreateReservationPageViewModel
{
    public SalesPanelShellViewModel Shell { get; set; } = new();
    public SalesReservationCreateModel Form { get; set; } = new();
    public List<SalesCustomerCardViewModel> Customers { get; set; } = new();
    public List<SalesSelectOption> Hotels { get; set; } = new();
    public List<SalesSelectOption> Cities { get; set; } = new();
    public List<SalesSelectOption> Districts { get; set; } = new();
    public List<SalesSelectOption> RoomTypes { get; set; } = new();
    public List<SalesRoomOptionViewModel> AvailableRooms { get; set; } = new();
    public List<SalesHotelSearchCardViewModel> HotelSearchResults { get; set; } = new();
    public SalesReservationPriceSummaryViewModel Summary { get; set; } = new();
    public string SelectedHotelSummary { get; set; } = string.Empty;
    public bool HasAssistantSearch { get; set; }
}

public class SalesCustomersPageViewModel
{
    public SalesPanelShellViewModel Shell { get; set; } = new();
    public List<SalesCustomerCardViewModel> Customers { get; set; } = new();
    public string? Search { get; set; }
}

public class SalesAvailabilityPageViewModel
{
    public SalesPanelShellViewModel Shell { get; set; } = new();
    public string? Search { get; set; }
    public long? SelectedHotelId { get; set; }
    public long? SelectedRoomTypeId { get; set; }
    public DateOnly SelectedMonth { get; set; }
    public List<SalesSelectOption> Hotels { get; set; } = new();
    public List<SalesSelectOption> RoomTypes { get; set; } = new();
    public List<SalesAvailabilityDayViewModel> Days { get; set; } = new();
    public string SelectedHotelLabel { get; set; } = string.Empty;
    public string SelectedRoomLabel { get; set; } = string.Empty;
    public string PreviousMonthQuery { get; set; } = string.Empty;
    public string NextMonthQuery { get; set; } = string.Empty;
}

public class SalesReservationsPageViewModel
{
    public SalesPanelShellViewModel Shell { get; set; } = new();
    public List<SalesReservationListItemViewModel> Reservations { get; set; } = new();
    public SalesReservationsFilterViewModel Filters { get; set; } = new();
    public SalesPaginationViewModel Pagination { get; set; } = new();
    public SalesReservationSummaryViewModel Summary { get; set; } = new();
}

public class SalesReportsPageViewModel
{
    public SalesPanelShellViewModel Shell { get; set; } = new();
    public decimal MonthlyRevenue { get; set; }
    public decimal MonthlyCommission { get; set; }
    public int MonthlyReservationCount { get; set; }
    public int MonthlyApprovedCount { get; set; }
    public int MonthlyCancelledCount { get; set; }
    public List<SalesMonthlyPerformanceItemViewModel> MonthlyBreakdown { get; set; } = new();
    public SalesPaginationViewModel Pagination { get; set; } = new();
    public int SelectedYear { get; set; } = DateTime.Today.Year;
}

public class SalesHotelGuidePageViewModel
{
    public SalesPanelShellViewModel Shell { get; set; } = new();
    public string? Search { get; set; }
    public List<SalesHotelGuideItemViewModel> Hotels { get; set; } = new();
}

public class SalesReservationCreateModel
{
    public long? CustomerId { get; set; }
    public string CustomerFullName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerCity { get; set; }
    public string? CustomerDistrict { get; set; }
    public string? CustomerNeighborhood { get; set; }
    public string? CustomerAddress { get; set; }
    public long HotelId { get; set; }
    public long RoomTypeId { get; set; }
    public string? SearchTerm { get; set; }
    public string? SearchCity { get; set; }
    public string? SearchDistrict { get; set; }
    public string? SearchNeighborhood { get; set; }
    public decimal? SearchMinPrice { get; set; }
    public decimal? SearchMaxPrice { get; set; }
    public decimal? SearchMinimumRating { get; set; }
    public int? SearchMinimumReviewCount { get; set; }
    public string? SearchFeature { get; set; }
    public DateOnly CheckInDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(2));
    public DateOnly CheckOutDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(4));
    public int AdultCount { get; set; } = 2;
    public int ChildCount { get; set; }
    public int RoomCount { get; set; } = 1;
    public string? DemandNote { get; set; }
}

public class SalesCustomerCreateModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? MembershipLevel { get; set; }
    public string? Note { get; set; }
}

public class SalesRoomOptionViewModel
{
    public long RoomTypeId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string FeaturesText { get; set; } = string.Empty;
    public string PriceText { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public string CapacityText { get; set; } = string.Empty;
    public string StockText { get; set; } = string.Empty;
}

public class SalesReservationPriceSummaryViewModel
{
    public string BaseAmountText { get; set; } = string.Empty;
    public string TaxAmountText { get; set; } = string.Empty;
    public string TotalAmountText { get; set; } = string.Empty;
    public decimal BaseNightlyAmount { get; set; }
    public decimal RoomTotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class SalesSelectOption
{
    public long Value { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class SalesHotelSearchCardViewModel
{
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string RatingText { get; set; } = string.Empty;
    public string ReviewCountText { get; set; } = string.Empty;
    public string PriceText { get; set; } = string.Empty;
    public string TodayDemandText { get; set; } = string.Empty;
    public string LocationText { get; set; } = string.Empty;
    public List<string> FeatureBadges { get; set; } = new();
}

public class SalesReservationsFilterViewModel
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Approval { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class SalesPaginationViewModel
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}

public class SalesReservationSummaryViewModel
{
    public int TotalCount { get; set; }
    public int ApprovedCount { get; set; }
    public int CancelledCount { get; set; }
    public string TotalRevenueText { get; set; } = string.Empty;
}

public class SalesMonthlyPerformanceItemViewModel
{
    public string PeriodLabel { get; set; } = string.Empty;
    public int ReservationCount { get; set; }
    public int ApprovedCount { get; set; }
    public int CancelledCount { get; set; }
    public string RevenueText { get; set; } = string.Empty;
}
