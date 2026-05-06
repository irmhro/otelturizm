using Microsoft.AspNetCore.Http;

namespace otelturizmnew.Models.Paneller.Partner;

public class PartnerShellViewModel
{
    public long UserId { get; set; }
    public long PartnerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserRole { get; set; } = "partner_staff";
    public string PanelTitle { get; set; } = string.Empty;
    public string PanelSubtitle { get; set; } = string.Empty;
    public string ActiveSectionKey { get; set; } = "dashboard";
    public long? SelectedHotelId { get; set; }
    public string SelectedHotelName { get; set; } = string.Empty;
    public int PendingReservations { get; set; }
    public int OpenSupportTickets { get; set; }
    public int LowStockAlerts { get; set; }
    public int UnansweredReviews { get; set; }
    public int FavoriteCount { get; set; }
    public string FavoriteSummaryText { get; set; } = string.Empty;
    public List<PartnerHotelSwitchItemViewModel> ManagedHotels { get; set; } = new();
    public PanelThemeViewModel Theme { get; set; } = new();
}

public sealed class PanelThemeViewModel
{
    public string BsTheme { get; set; } = "light"; // light/dark/auto
    public string? PrimaryHex { get; set; }
    public string? AccentHex { get; set; }
    public string? SidebarBgHex { get; set; }
    public decimal? RadiusScale { get; set; }
    public string? Density { get; set; } // compact/normal/cozy
    public string? FontFamily { get; set; }
    public string? LayoutMode { get; set; } // vertical/horizontal/condensed
    public bool Rtl { get; set; }
}

public class PartnerHotelSwitchItemViewModel
{
    public long HotelId { get; set; }
    public string HotelCode { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string CityLabel { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsSelected { get; set; }
}

public class PartnerStatCardViewModel
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-chart-line";
    public string ToneClass { get; set; } = "info";
}

public class PartnerAlertViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-circle-info";
    public string ToneClass { get; set; } = "info";
}

public class PartnerDashboardViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public List<PartnerStatCardViewModel> SummaryCards { get; set; } = new();
    public List<PartnerStatCardViewModel> WidgetCards { get; set; } = new();
    public List<PartnerRevenuePointViewModel> RevenueTrend { get; set; } = new();
    public List<PartnerReservationRowViewModel> UpcomingReservations { get; set; } = new();
    public List<PartnerReservationRowViewModel> RecentReservations { get; set; } = new();
    public List<PartnerInventoryAlertViewModel> InventoryAlerts { get; set; } = new();
    public List<PartnerReviewRowViewModel> RecentReviews { get; set; } = new();
    public List<PartnerQuickActionViewModel> QuickActions { get; set; } = new();
    public PartnerReservationFilterViewModel Filters { get; set; } = new();
    public int DashboardReservationTotalCount { get; set; }
    public int DashboardPageSize { get; set; } = 7;
    public long? SelectedConversationId { get; set; }
    public List<PartnerConversationSummaryViewModel> Conversations { get; set; } = new();
    public List<PartnerConversationMessageViewModel> ConversationMessages { get; set; } = new();
    public PartnerGuestMessageRequest MessageForm { get; set; } = new();
    public int RejectCountLast30Days { get; set; }
    public bool PenaltyActive { get; set; }
    public string? PenaltyEndText { get; set; }
}

public class PartnerQuickActionViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-bolt";
    public string Url { get; set; } = "#";
    public string ToneClass { get; set; } = "info";
}

public class PartnerRevenuePointViewModel
{
    public string Label { get; set; } = string.Empty;
    public decimal RevenueAmount { get; set; }
    public int ReservationCount { get; set; }
    public int HeightPercent { get; set; }
}

public class PartnerInventoryAlertViewModel
{
    public long RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public string AvailabilityText { get; set; } = string.Empty;
    public string ToneClass { get; set; } = "warning";
}

public class PartnerReservationRowViewModel
{
    public long ReservationId { get; set; }
    public string ReservationNo { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string GuestEmail { get; set; } = string.Empty;
    public string GuestPhone { get; set; } = string.Empty;
    public string? RoomName { get; set; }
    public string StayText { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string PaymentStatusLabel { get; set; } = string.Empty;
    public string TotalText { get; set; } = string.Empty;
    public string CreatedText { get; set; } = string.Empty;
    public string? PaymentMethodLabel { get; set; }
    /// <summary>Karma ödeme kalemleri özeti (kart / havale tutarları).</summary>
    public string? PaymentMixSummary { get; set; }
    public int PaymentLineCount { get; set; }
    public string? SourceLabel { get; set; }
    public string? GuestNote { get; set; }
    public string? RequestNote { get; set; }
    public string? CancellationReason { get; set; }
    public string? CancellationTimeText { get; set; }
    public byte AdultCount { get; set; }
    public byte ChildCount { get; set; }
    public short NightCount { get; set; }
    public bool CanApprove { get; set; }
    public bool CanReject { get; set; }
    public bool CanCheckIn { get; set; }
    public bool CanMarkPaymentCompleted { get; set; }
    public bool CanMessageGuest { get; set; }
    public bool CanOpenDetails { get; set; } = true;
    public string StatusTone { get; set; } = "pending";
}

public class PartnerReservationsPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public List<PartnerStatCardViewModel> SummaryCards { get; set; } = new();
    public List<PartnerReservationRowViewModel> Reservations { get; set; } = new();
    public PartnerReservationFilterViewModel Filters { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; }
    public int RejectCountLast30Days { get; set; }
    public bool PenaltyActive { get; set; }
    public string? PenaltyEndText { get; set; }
    public long? SelectedConversationId { get; set; }
    public List<PartnerConversationSummaryViewModel> Conversations { get; set; } = new();
    public List<PartnerConversationMessageViewModel> ConversationMessages { get; set; } = new();
    public PartnerReservationStatusRequest StatusForm { get; set; } = new();
    public PartnerGuestMessageRequest MessageForm { get; set; } = new();
}

public class PartnerConversationSummaryViewModel
{
    public long ConversationId { get; set; }
    public long ReservationId { get; set; }
    public string ReservationNo { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string LastMessagePreview { get; set; } = string.Empty;
    public string LastMessageTimeText { get; set; } = string.Empty;
    public int UnreadCount { get; set; }
    public bool IsSelected { get; set; }
}

public class PartnerConversationMessageViewModel
{
    public long MessageId { get; set; }
    public string SenderLabel { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
    public bool IsFromHotel { get; set; }
}

public class PartnerReservationFilterViewModel
{
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public string Status { get; set; } = "all";
    public string PaymentMethod { get; set; } = "all";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PartnerReservationStatusRequest
{
    public long HotelId { get; set; }
    public long ReservationId { get; set; }
    public string ActionType { get; set; } = "approve";
    public string? Reason { get; set; }
    public string? ReturnUrl { get; set; }
}

public class PartnerGuestMessageRequest
{
    public long HotelId { get; set; }
    public long ReservationId { get; set; }
    public long? ConversationId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
}

public class PartnerGuestMessagesPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public long SelectedHotelId { get; set; }
    public long? SelectedConversationId { get; set; }
    public List<PartnerConversationSummaryViewModel> Conversations { get; set; } = new();
    public List<PartnerConversationMessageViewModel> ConversationMessages { get; set; } = new();
    public PartnerGuestMessageRequest MessageForm { get; set; } = new();
}

public class PartnerReservationCalendarPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public long SelectedHotelId { get; set; }
    public string RangeLabel { get; set; } = string.Empty;
    public string DateFrom { get; set; } = string.Empty;
    public string DateTo { get; set; } = string.Empty;
    public List<PartnerReservationCalendarDayRowViewModel> Days { get; set; } = new();
    public List<PartnerReservationRowViewModel> UpcomingCheckins { get; set; } = new();
    public List<PartnerReservationRowViewModel> UpcomingCheckouts { get; set; } = new();
}

public class PartnerReservationCalendarDayRowViewModel
{
    public DateOnly Date { get; set; }
    public string DayLabel { get; set; } = string.Empty;
    public string WeekdayLabel { get; set; } = string.Empty;
    public int CheckinCount { get; set; }
    public int CheckoutCount { get; set; }
    public int InhouseCount { get; set; }
    public int CancelledCount { get; set; }
}

public class PartnerCancellationNoShowPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public long SelectedHotelId { get; set; }
    public string? CancellationSummary { get; set; }
    public string? CancellationDetails { get; set; }
    public byte? FreeCancellationDays { get; set; }
    public decimal? LateCancellationPenaltyPercent { get; set; }
    public decimal? NoShowPenaltyPercent { get; set; }
    public int CancelCountLast30Days { get; set; }
    public int RejectCountLast30Days { get; set; }
    public List<(string Reason, int Count)> TopCancellationReasons { get; set; } = new();
}

public class PartnerPaymentStatusesPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public long SelectedHotelId { get; set; }
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    public string PaymentStatus { get; set; } = "all";
    public string PaymentMethod { get; set; } = "all";
    public List<PartnerPaymentStatusRowViewModel> Rows { get; set; } = new();
}

public class PartnerPaymentStatusRowViewModel
{
    public long ReservationId { get; set; }
    public string ReservationNo { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string StayText { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string PaymentStatusLabel { get; set; } = string.Empty;
    public string PaymentMethodLabel { get; set; } = string.Empty;
    public string TotalText { get; set; } = string.Empty;
    public string CollectedText { get; set; } = string.Empty;
    public string RemainingText { get; set; } = string.Empty;
    public string LastPaymentTimeText { get; set; } = string.Empty;
    public string ToneClass { get; set; } = "secondary";
}

public class PartnerCompanyReservationsPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public long SelectedHotelId { get; set; }
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
    /// <summary>creation = oluşturulma tarihi; stay = giriş/çıkış (konaklama penceresi).</summary>
    public string DateRangeMode { get; set; } = "creation";
    /// <summary>Çıkış tarihi geçmiş ve iptal dışı kayıtlar (konaklaması bitmiş).</summary>
    public bool CompletedStaysOnly { get; set; }
    public long? CompanyId { get; set; }
    public string Status { get; set; } = "all";
    public List<PartnerCompanyOptionViewModel> CompanyOptions { get; set; } = new();
    public List<PartnerCompanyReservationRowViewModel> Rows { get; set; } = new();
}

public class PartnerCompanyReservationRowViewModel
{
    public long ReservationId { get; set; }
    public string ReservationNo { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string StayText { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string CompanyApprovalStatus { get; set; } = string.Empty;
    public string TotalText { get; set; } = string.Empty;
    public string CreatedText { get; set; } = string.Empty;
}

public class PartnerCompanyAnalyticsPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public long SelectedHotelId { get; set; }
    public string RangeLabel { get; set; } = "Son 90 gün";
    public List<PartnerCompanyAnalyticsRowViewModel> Rows { get; set; } = new();
}

public class PartnerCompanyAnalyticsRowViewModel
{
    public long CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int ReservationCount { get; set; }
    public string RevenueText { get; set; } = string.Empty;
    public string AvgTicketText { get; set; } = string.Empty;
    public string LastReservationText { get; set; } = string.Empty;
}

public class PartnerCompanyRequestsPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public long SelectedHotelId { get; set; }
    public List<PartnerCompanyRequestRowViewModel> Companies { get; set; } = new();
    public List<PartnerCompanyRequestActivityRowViewModel> Activities { get; set; } = new();
}

public class PartnerCompanyRequestRowViewModel
{
    public long CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedText { get; set; } = string.Empty;
    public string ContactText { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public class PartnerCompanyRequestActivityRowViewModel
{
    public long ActivityId { get; set; }
    public long CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class PartnerPricingPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public long SelectedHotelId { get; set; }
    public long? SelectedRoomId { get; set; }
    public string MonthKey { get; set; } = string.Empty;
    public string MonthLabel { get; set; } = string.Empty;
    public string PreviousMonthKey { get; set; } = string.Empty;
    public string NextMonthKey { get; set; } = string.Empty;
    public List<PartnerMonthOptionViewModel> MonthOptions { get; set; } = new();
    public List<PartnerRoomSummaryViewModel> Rooms { get; set; } = new();
    public List<PartnerStatCardViewModel> SummaryCards { get; set; } = new();
    public List<PartnerPricingDayViewModel> CalendarDays { get; set; } = new();
    public List<PartnerDiscountOptionViewModel> AvailableDiscounts { get; set; } = new();
    public PartnerBulkPricingUpdateRequest BulkForm { get; set; } = new();
    public PartnerDailyPricingUpdateRequest DailyForm { get; set; } = new();
}

public class PartnerMonthOptionViewModel
{
    public string MonthKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
}

public class PartnerPricingDayViewModel
{
    public DateOnly Date { get; set; }
    public string DayLabel { get; set; } = string.Empty;
    public string WeekdayLabel { get; set; } = string.Empty;
    public string PriceText { get; set; } = string.Empty;
    public string BasePriceText { get; set; } = string.Empty;
    public string DiscountPriceText { get; set; } = string.Empty;
    public string AvailabilityText { get; set; } = string.Empty;
    public string SoldText { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public decimal BasePriceAmount { get; set; }
    public decimal? DiscountPriceAmount { get; set; }
    public short TotalRooms { get; set; }
    public short SoldRooms { get; set; }
    public short BlockedRooms { get; set; }
    public byte MinStay { get; set; }
    public short MaxStay { get; set; }
    public long? DiscountId { get; set; }
    public string? CampaignLabel { get; set; }
    public string? DiscountPercentText { get; set; }
    public string ToneClass { get; set; } = "open";
    public string? PriceNote { get; set; }
    public bool IsClosed { get; set; }
    public bool IsHighlighted { get; set; }
    public bool IsToday { get; set; }
    public bool IsCurrentMonth { get; set; } = true;
    public bool HasDiscount { get; set; }
}

public class PartnerBulkPricingUpdateRequest
{
    public long HotelId { get; set; }
    public long? RoomId { get; set; }
    public long? ViewRoomId { get; set; }
    public string? ViewMonth { get; set; }
    public List<long> SelectedRoomIds { get; set; } = new();
    public DateTime DateFrom { get; set; } = DateTime.Today;
    public DateTime DateTo { get; set; } = DateTime.Today.AddDays(14);
    public decimal? BasePrice { get; set; }
    public decimal? DiscountPrice { get; set; }
    public bool ClearDiscountPrice { get; set; }
    public short? TotalRooms { get; set; }
    public byte? MinStay { get; set; } = 1;
    public short? MaxStay { get; set; } = 30;
    public string? SaleStatusAction { get; set; }
    public bool CloseSale { get; set; }
    public bool OpenSale { get; set; }
    public long? DiscountId { get; set; }
    public string? CampaignLabel { get; set; }
    public string? PriceNote { get; set; }
}

public class PartnerDailyPricingUpdateRequest
{
    public long HotelId { get; set; }
    public long RoomId { get; set; }
    public string? ViewMonth { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public decimal? BasePrice { get; set; }
    public decimal? DiscountPrice { get; set; }
    public bool ClearDiscountPrice { get; set; }
    public short? TotalRooms { get; set; }
    public byte? MinStay { get; set; } = 1;
    public short? MaxStay { get; set; } = 30;
    public string? SaleStatusAction { get; set; } = "keep";
    public long? DiscountId { get; set; }
    public string? PriceNote { get; set; }
}

public class PartnerDiscountOptionViewModel
{
    public long DiscountId { get; set; }
    public string DiscountName { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? ImageUrl { get; set; }
    public string? IconClass { get; set; }
    public string? ColorCode { get; set; }
}

public class PartnerCampaignOptionViewModel
{
    public long CampaignId { get; set; }
    public string CampaignCode { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
    public string CampaignType { get; set; } = string.Empty;
    public string DiscountText { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? BadgeText { get; set; }
    public string? PromoBadge { get; set; }
    public string? ColorCode { get; set; }
    public bool IsHighlighted { get; set; }
}

public class PartnerCampaignParticipationViewModel
{
    public long ParticipationId { get; set; }
    public long CampaignId { get; set; }
    public string CampaignCode { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public string? CampaignLabel { get; set; }
    public string? PartnerNote { get; set; }
    public string? DiscountText { get; set; }
    public bool IsFeatured { get; set; }
}

public class PartnerCampaignsPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public long SelectedHotelId { get; set; }
    public List<PartnerStatCardViewModel> SummaryCards { get; set; } = new();
    public List<PartnerCampaignOptionViewModel> AvailableCampaigns { get; set; } = new();
    public List<PartnerCampaignParticipationViewModel> JoinedCampaigns { get; set; } = new();
    public PartnerCampaignJoinRequest JoinForm { get; set; } = new();
}

public class PartnerCampaignJoinRequest
{
    public long HotelId { get; set; }
    public long CampaignId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? CustomDiscountRate { get; set; }
    public decimal? CustomDiscountAmount { get; set; }
    public decimal? CustomCampaignPrice { get; set; }
    public string? CampaignLabel { get; set; }
    public string? LandingUrl { get; set; }
    public string? PartnerNote { get; set; }
    public bool Featured { get; set; }
    public int SortOrder { get; set; }
}

public class PartnerRoomManagementPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public long SelectedHotelId { get; set; }
    public long? SelectedRoomId { get; set; }
    public List<PartnerRoomSummaryViewModel> Rooms { get; set; } = new();
    public List<PartnerRoomInventoryRowViewModel> InventoryRows { get; set; } = new();
    public List<PartnerRoomPhotoCardViewModel> SelectedRoomPhotos { get; set; } = new();
    public List<PartnerRoomFeatureRowViewModel> AvailableRoomFeatures { get; set; } = new();
    public List<long> SelectedRoomFeatureIds { get; set; } = new();
    public PartnerRoomUpsertRequest Form { get; set; } = new();
    public PartnerRoomPhotoUploadRequest PhotoUploadForm { get; set; } = new();
    public bool IsEditingRoom => Form.RoomId.HasValue;
}

public class PartnerRoomSummaryViewModel
{
    public long RoomId { get; set; }
    /// <summary>DB saklanan standart gece net tutari (vergisiz taban).</summary>
    public decimal StandardNightlyStoredNet { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string CapacityText { get; set; } = string.Empty;
    public string StockText { get; set; } = string.Empty;
    public string BasePriceText { get; set; } = string.Empty;
    public string DiscountPriceText { get; set; } = string.Empty;
    public int MonthlyReservationCount { get; set; }
    public int MonthlyDiscountDayCount { get; set; }
    public string ToneClass { get; set; } = "tone-1";
    public string? CoverPhotoUrl { get; set; }
    public bool IsActive { get; set; }
}

public class PartnerRoomInventoryRowViewModel
{
    public long RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public short TotalRooms { get; set; }
    public short SellableRooms { get; set; }
    public short MaintenanceRooms { get; set; }
    public string MinPriceText { get; set; } = string.Empty;
    public string MaxPriceText { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class PartnerRoomUpsertRequest
{
    public long HotelId { get; set; }
    public long? RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string RoomCategory { get; set; } = "Standart";
    public string? Description { get; set; }
    public string? BedType { get; set; }
    public string? ViewType { get; set; }
    public short? RoomSize { get; set; }
    public byte MaxAdults { get; set; } = 2;
    public byte MaxChildren { get; set; }
    public byte MaxBabies { get; set; }
    public short TotalRooms { get; set; } = 1;
    public decimal BasePrice { get; set; }
    public decimal? DiscountPrice { get; set; }
    public string? CoverPhotoPath { get; set; }
    public string? RoomFeaturesText { get; set; }
    public List<long> SelectedFeatureIds { get; set; } = new();
    public bool IsActive { get; set; } = true;
}

public class PartnerRoomPhotoCardViewModel
{
    public long PhotoId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ushort DisplayOrder { get; set; }
    public bool IsCover { get; set; }
    public bool IsApproved { get; set; }
}

public class PartnerRoomPhotoUploadRequest
{
    public long HotelId { get; set; }
    public long RoomId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public bool MakeCover { get; set; }
    public ushort DisplayOrder { get; set; }
    public List<IFormFile> Files { get; set; } = new();
}

public class PartnerHotelInfoPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public PartnerHotelInfoForm Form { get; set; } = new();
    public List<PartnerAmenityOptionViewModel> AvailableAmenities { get; set; } = new();
    public List<PartnerHotelTypeOptionViewModel> HotelTypes { get; set; } = new();
}

public class PartnerHotelInfoForm
{
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public int? HotelTypeId { get; set; }
    public string HotelType { get; set; } = "Hotel";
    public string? TourismDocumentNo { get; set; }
    public string? TourismDocumentType { get; set; }
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string? Address { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Neighborhood { get; set; }
    public string? PostalCode { get; set; }
    public string? LocationDescription { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Website { get; set; }
    public string? ContactEmail { get; set; }
    public string? HotelPhone { get; set; }
    public string? HotelPhone2 { get; set; }
    public string? ReservationPhone { get; set; }
    public string? Fax { get; set; }
    public string? SalesContactName { get; set; }
    public string? SalesContactPhone { get; set; }
    public string? SalesContactEmail { get; set; }
    public string? SalesNotes { get; set; }
    public string? CheckInTime { get; set; }
    public string? CheckOutTime { get; set; }
    public bool LateCheckoutAvailable { get; set; }
    public decimal? LateCheckoutFee { get; set; }
    public bool EarlyCheckinAvailable { get; set; }
    public decimal? EarlyCheckinFee { get; set; }
    public byte MinStay { get; set; } = 1;
    public short MaxStay { get; set; } = 30;
    public byte? StarCount { get; set; }
    public short TotalRoomCount { get; set; }
    public short? TotalBedCapacity { get; set; }
    public byte? FloorCount { get; set; }
    public bool ElevatorAvailable { get; set; }
    public byte ElevatorCount { get; set; }
    public decimal DefaultCommissionRate { get; set; }
    public string? CommissionType { get; set; }
    public string? CommissionCalculationType { get; set; }
    public string? PaymentTerm { get; set; }
    public string? PaymentMethod { get; set; }
    public string? InvoiceIssueType { get; set; }
    public decimal? DepositAmount { get; set; }
    public byte? DepositReturnDays { get; set; }
    public string? SpokenLanguages { get; set; }
    public string? VideoUrl { get; set; }
    public string? VirtualTourUrl { get; set; }
    public string? PublishStatus { get; set; }
    public string? ApprovalStatus { get; set; }
    public decimal? AverageScore { get; set; }
    public int TotalReviewCount { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<long> SelectedAmenityIds { get; set; } = new();
}

public class PartnerHotelAmenitiesUpdateRequest
{
    public long HotelId { get; set; }
    public List<long> SelectedAmenityIds { get; set; } = new();
}

public class PartnerHotelLocationUpdateRequest
{
    public long HotelId { get; set; }
    public long? CityId { get; set; }
    public long? DistrictId { get; set; }
    public long? NeighborhoodId { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Neighborhood { get; set; }
    public string? PostalCode { get; set; }
    public string? Address { get; set; }
    public string? LocationDescription { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}

public class PartnerLocationOptionViewModel
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class PartnerHotelLocationPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public PartnerHotelInfoForm Form { get; set; } = new();

    public List<PartnerLocationOptionViewModel> Cities { get; set; } = new();
    public List<PartnerLocationOptionViewModel> Districts { get; set; } = new();
    public List<PartnerLocationOptionViewModel> Neighborhoods { get; set; } = new();

    public long? SelectedCityId { get; set; }
    public long? SelectedDistrictId { get; set; }
    public long? SelectedNeighborhoodId { get; set; }
}

public class PartnerHotelTypeOptionViewModel
{
    public int HotelTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-hotel";
}

public class PartnerAmenityOptionViewModel
{
    public long AmenityId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-circle-check";
    public string CategoryName { get; set; } = "Genel";
    public bool IsSelected { get; set; }
}

public class PartnerHotelPoliciesPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public long SelectedHotelId { get; set; }
    public PartnerHotelPoliciesForm Form { get; set; } = new();
    public List<PartnerHotelPolicyOptionViewModel> PolicyOptions { get; set; } = new();
}

public class PartnerHotelPolicyOptionViewModel
{
    public long PolicyId { get; set; }
    public string Category { get; set; } = "Genel";
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short Order { get; set; } = 100;
    public bool IsSelected { get; set; }
}

public class PartnerHotelPoliciesForm
{
    public long HotelId { get; set; }
    public List<long> SelectedPolicyIds { get; set; } = new();
    public string? SmokingPolicy { get; set; }
    public string? PetPolicy { get; set; }
    public decimal? PetFee { get; set; }
    public decimal? PetDeposit { get; set; }
    public bool PartyAllowed { get; set; }
    public string? QuietHoursStart { get; set; }
    public string? QuietHoursEnd { get; set; }
    public byte? MinimumAgeLimit { get; set; }
    public bool AdultsOnly { get; set; }
    public string? ChildAgeRange { get; set; }
    public bool BabyCribAvailable { get; set; }
    public decimal? BabyCribFee { get; set; }
    public bool ExtraBedAvailable { get; set; }
    public decimal? ExtraBedFee { get; set; }
    public byte? MaxChildren { get; set; }
    public bool PrepaymentRequired { get; set; } = true;
    public decimal? PrepaymentPercent { get; set; } = 30m;
    public string? RemainingPaymentTimeText { get; set; }
    public bool CreditCardPaymentAccepted { get; set; } = true;
    public bool CashPaymentAccepted { get; set; }
    public string? AcceptedCardsText { get; set; }
    public string? CancellationSummary { get; set; }
    public string? CancellationDetails { get; set; }
    public byte? FreeCancellationDays { get; set; }
    public decimal? LateCancellationPenaltyPercent { get; set; }
    public decimal? NoShowPenaltyPercent { get; set; }
    public decimal? DamageDepositAmount { get; set; }
    public string? DamageDepositDescription { get; set; }
    public bool OutsideFoodAllowed { get; set; } = true;
    public bool VisitorAllowed { get; set; }
    public string? VisitorHoursStart { get; set; }
    public string? VisitorHoursEnd { get; set; }
    public string? SpecialConditions { get; set; }
}

public class PartnerRoomFeaturesPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public long SelectedHotelId { get; set; }
    public List<PartnerRoomFeatureRowViewModel> Features { get; set; } = new();
    public PartnerRoomFeatureAddRequest AddForm { get; set; } = new();
}

public class PartnerRoomFeatureRowViewModel
{
    public short FeatureId { get; set; }
    public string Category { get; set; } = "Genel";
    public string Name { get; set; } = string.Empty;
    public string? IconClass { get; set; }
    public short Order { get; set; }
    public bool IsActive { get; set; }
}

public class PartnerRoomFeatureAddRequest
{
    public long HotelId { get; set; }
    public string Category { get; set; } = "Genel";
    public string Name { get; set; } = string.Empty;
    public string? IconClass { get; set; }
    public short Order { get; set; }
}

public class PartnerRoomFeatureToggleRequest
{
    public long HotelId { get; set; }
    public short FeatureId { get; set; }
    public bool IsActive { get; set; }
}

public class PartnerPhotosPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public long SelectedHotelId { get; set; }
    public List<PartnerStatCardViewModel> SummaryCards { get; set; } = new();
    public List<PartnerPhotoCardViewModel> Photos { get; set; } = new();
    public List<PartnerPhotoRoomLinkViewModel> Rooms { get; set; } = new();
    public PartnerPhotoUploadRequest UploadForm { get; set; } = new();
    public PartnerPhotoEditForm EditForm { get; set; } = new();
    public PartnerPhotoBulkDeleteRequest BulkDeleteForm { get; set; } = new();
    public bool IsEditingPhoto => EditForm.PhotoId.HasValue;
}

public class PartnerPhotoRoomLinkViewModel
{
    public long RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string? CoverPhotoUrl { get; set; }
    public short TotalRooms { get; set; }
    public bool IsActive { get; set; }
}

public class PartnerPhotoCardViewModel
{
    public long PhotoId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string SortText { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ushort DisplayOrder { get; set; }
    public bool IsCover { get; set; }
    public bool IsApproved { get; set; }
}

public class PartnerPhotoUploadRequest
{
    public long HotelId { get; set; }
    public string PhotoType { get; set; } = "Genel Alan";
    public string? Title { get; set; }
    public string? Description { get; set; }
    public bool MakeCover { get; set; }
    public ushort DisplayOrder { get; set; }
    public List<IFormFile> Files { get; set; } = new();
}

public class PartnerPhotoEditForm
{
    public long HotelId { get; set; }
    public long? PhotoId { get; set; }
    public string PhotoType { get; set; } = "Genel Alan";
    public string? Title { get; set; }
    public string? Description { get; set; }
    public ushort DisplayOrder { get; set; }
    public bool MarkAsFeatured { get; set; }
}

public class PartnerPhotoBulkDeleteRequest
{
    public long HotelId { get; set; }
    public List<long> PhotoIds { get; set; } = new();
}

public class PartnerPerformancePageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public List<PartnerStatCardViewModel> SummaryCards { get; set; } = new();
    public List<PartnerRevenuePointViewModel> RevenueTrend { get; set; } = new();
    public string InfoNote { get; set; } = string.Empty;
    public List<PartnerCompetitorRowViewModel> Competitors { get; set; } = new();
    public PartnerCompetitorUpsertRequest CompetitorForm { get; set; } = new();
}

public class PartnerCompetitorRowViewModel
{
    public long CompetitorId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string LocationText { get; set; } = string.Empty;
    public string AnalysisDateText { get; set; } = string.Empty;
    public string AveragePriceText { get; set; } = string.Empty;
    public string OccupancyText { get; set; } = string.Empty;
    public string? SourceUrl { get; set; }
    public string? Notes { get; set; }
}

public class PartnerCompetitorUpsertRequest
{
    public long HotelId { get; set; }
    public long? CompetitorId { get; set; }
    public string CompetitorHotelName { get; set; } = string.Empty;
    public string? CompetitorCity { get; set; }
    public string? CompetitorDistrict { get; set; }
    public DateTime AnalysisDate { get; set; } = DateTime.Today;
    public decimal? AverageNightlyPrice { get; set; }
    public decimal? EstimatedOccupancyRate { get; set; }
    public string? SourceUrl { get; set; }
    public string? Notes { get; set; }
}

public class PartnerReviewRowViewModel
{
    public long ReviewId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ScoreText { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string CreatedText { get; set; } = string.Empty;
    public DateTime? CreatedAtUtc { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string? ResponseText { get; set; }
}

public class PartnerReviewsPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public List<PartnerStatCardViewModel> SummaryCards { get; set; } = new();
    public List<PartnerReviewRowViewModel> Reviews { get; set; } = new();
    public PartnerReviewReplyRequest ReplyForm { get; set; } = new();
    public string StatusFilter { get; set; } = string.Empty;
    public string ReplyStateFilter { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 7;
    public int TotalCount { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)Math.Max(1, PageSize)));
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

public class PartnerReviewReplyRequest
{
    public long HotelId { get; set; }
    public long ReviewId { get; set; }
    public string ResponseText { get; set; } = string.Empty;
}

public class PartnerReviewReportRequest
{
    public long HotelId { get; set; }
    public long ReviewId { get; set; }
}

public class PartnerFinancePageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public List<PartnerStatCardViewModel> SummaryCards { get; set; } = new();
    public List<PartnerFinanceTransactionViewModel> Transactions { get; set; } = new();
    public List<PartnerFinanceInvoiceViewModel> Invoices { get; set; } = new();
    public PartnerBankInfoForm BankInfoForm { get; set; } = new();
    public string PayoutNote { get; set; } = string.Empty;
    public string ActiveRuleDateText { get; set; } = string.Empty;
    public string ActiveRuleTaxText { get; set; } = string.Empty;
    public string ActiveRuleCommissionText { get; set; } = string.Empty;
    public List<PartnerFinanceTaxRowViewModel> TaxRows { get; set; } = new();
}

public class PartnerFinanceTransactionViewModel
{
    public long ReservationId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string AmountText { get; set; } = string.Empty;
    public string DetailText { get; set; } = string.Empty;
}

public class PartnerFinanceTaxRowViewModel
{
    public string Label { get; set; } = string.Empty;
    public string RateText { get; set; } = string.Empty;
    public string AmountText { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class PartnerFinanceInvoiceViewModel
{
    public long InvoiceId { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public string InvoiceDateText { get; set; } = string.Empty;
    public string InvoiceTypeText { get; set; } = string.Empty;
    public string InvoiceStatusText { get; set; } = string.Empty;
    public string TotalText { get; set; } = string.Empty;
    public string RecipientText { get; set; } = string.Empty;
}

public class PartnerReservationGuestInvoiceRowViewModel
{
    public long ReservationId { get; set; }
    public string ReservationNo { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string StayText { get; set; } = string.Empty;
    public string TotalText { get; set; } = string.Empty;
    public bool HasInvoice { get; set; }
    public long? SecureFileId { get; set; }
    public string? DownloadUrl { get; set; }
    public string? UploadedAtText { get; set; }
}

public class PartnerReservationGuestInvoicesPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public List<PartnerReservationGuestInvoiceRowViewModel> Rows { get; set; } = new();
}

public class PartnerBankInfoForm
{
    public long HotelId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string? BranchName { get; set; }
    public string Iban { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string Currency { get; set; } = "TRY";
}

public class PartnerPreferencesPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public PartnerPreferencesForm Form { get; set; } = new();
}

public class PartnerPreferencesForm
{
    public long HotelId { get; set; }
    public long? DefaultHotelId { get; set; }
    public string Language { get; set; } = "tr";
    public string Currency { get; set; } = "TRY";
    public string Timezone { get; set; } = "Europe/Istanbul";
    public string CalendarView { get; set; } = "Aylik";
    public bool EmailNotifications { get; set; } = true;
    public bool SmsNotifications { get; set; }
    public bool PushNotifications { get; set; } = true;
    public bool DesktopNotifications { get; set; } = true;
    public bool NewReservationNotifications { get; set; } = true;
    public bool CancellationNotifications { get; set; } = true;
    public bool PaymentNotifications { get; set; } = true;
    public bool ReviewNotifications { get; set; } = true;
    public bool AutoPriceSuggestions { get; set; } = true;
    public bool AutoCloseoutWarnings { get; set; } = true;
    public bool RememberDevice { get; set; } = true;
}

public class PartnerApplicationPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public PartnerApplicationStatusViewModel Status { get; set; } = new();
    public PartnerApplicationProfileForm Form { get; set; } = new();
    public PartnerApplicationDocumentUploadForm UploadForm { get; set; } = new();
    public List<PartnerApplicationDocumentViewModel> Documents { get; set; } = new();
    public List<PartnerHotelTypeOptionViewModel> HotelTypes { get; set; } = new();
}

public class PartnerApplicationStatusViewModel
{
    public long PartnerId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string StatusToneClass { get; set; } = "warning";
    public string RegistrationDateText { get; set; } = string.Empty;
    public string? ApprovalDateText { get; set; }
    public string? RejectionReason { get; set; }
    public bool EmailVerified { get; set; }
    public bool CanPublish { get; set; }
    public string PublicationHint { get; set; } = string.Empty;
}

public class PartnerApplicationProfileForm
{
    public long HotelId { get; set; }
    public long PartnerId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyType { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public int? HotelTypeId { get; set; }
    public string HotelType { get; set; } = "Otel";
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
    public string? Neighborhood { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string? BankBranch { get; set; }
    public string Iban { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? Description { get; set; }
}

public class PartnerApplicationDocumentUploadForm
{
    public long HotelId { get; set; }
    public long PartnerId { get; set; }
    public string DocumentType { get; set; } = "Vergi Levhasi";
    public string? DocumentTitle { get; set; }
    public IFormFile? File { get; set; }
}

public class PartnerApplicationDocumentViewModel
{
    public long DocumentId { get; set; }
    public long SecureFileId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string StatusToneClass { get; set; } = "warning";
    public string UploadedAtText { get; set; } = string.Empty;
    public string? ReviewNote { get; set; }
    public string? AccessUrl { get; set; }
}

public class PartnerSupportPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public List<PartnerStatCardViewModel> SummaryCards { get; set; } = new();
    public List<PartnerSupportTicketViewModel> Tickets { get; set; } = new();
    public List<PartnerKnowledgeBaseArticleViewModel> KnowledgeBaseArticles { get; set; } = new();
    public List<PartnerSupportChannelViewModel> Channels { get; set; } = new();
    public PartnerSupportCreateTicketRequest CreateTicketForm { get; set; } = new();
    public long? SelectedTicketId { get; set; }
    public PartnerSupportTicketDetailViewModel? SelectedTicket { get; set; }
    public List<PartnerSupportMessageViewModel> Messages { get; set; } = new();
    public PartnerSupportSendMessageRequest SendMessageForm { get; set; } = new();
}

public class PartnerSupportTicketViewModel
{
    public long TicketId { get; set; }
    public string TicketNo { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string UpdatedText { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}

public class PartnerSupportTicketDetailViewModel
{
    public long TicketId { get; set; }
    public string TicketNo { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedText { get; set; } = string.Empty;
    public string UpdatedText { get; set; } = string.Empty;
}

public class PartnerSupportMessageViewModel
{
    public long MessageId { get; set; }
    public string SenderLabel { get; set; } = "Partner";
    public string Body { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
    public bool IsFromPartner { get; set; }
    public string? AttachmentUrl { get; set; }
}

public class PartnerKnowledgeBaseArticleViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class PartnerSupportChannelViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-headset";
}

public class PartnerSupportCreateTicketRequest
{
    public long HotelId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Category { get; set; } = "Operasyon";
    public string Priority { get; set; } = "Normal";
    public string Message { get; set; } = string.Empty;
}

public class PartnerSupportSendMessageRequest
{
    public long HotelId { get; set; }
    public long TicketId { get; set; }
    public string Message { get; set; } = string.Empty;
    public IFormFile? File { get; set; }
}

public sealed class PartnerLocationInsightsPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public long SelectedHotelId { get; set; }
    public string HotelLocationLabel { get; set; } = string.Empty;
    public string InfoNote { get; set; } = string.Empty;
    public List<PartnerStatCardViewModel> SummaryCards { get; set; } = new();
    public List<PartnerGuestCityInsightRowViewModel> GuestCityRows { get; set; } = new();
    public List<PartnerGuestCountryInsightRowViewModel> GuestCountryRows { get; set; } = new();
    public List<PartnerDailyHotelStatRowViewModel> DailyStatRows { get; set; } = new();
}

public sealed class PartnerGuestCityInsightRowViewModel
{
    public string CityLabel { get; set; } = string.Empty;
    public int ReservationCount { get; set; }
    public decimal SharePercent { get; set; }
}

public sealed class PartnerGuestCountryInsightRowViewModel
{
    public string CountryLabel { get; set; } = string.Empty;
    public int ReservationCount { get; set; }
    public decimal SharePercent { get; set; }
}

public sealed class PartnerDailyHotelStatRowViewModel
{
    public string DateText { get; set; } = string.Empty;
    public int Reservations { get; set; }
    public decimal? OccupancyPercent { get; set; }
    public decimal? NetRevenue { get; set; }
}

public sealed class PartnerFavoriteGuestsPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public long SelectedHotelId { get; set; }
    public string PrivacyNote { get; set; } = string.Empty;
    public List<PartnerStatCardViewModel> SummaryCards { get; set; } = new();
    public List<PartnerFavoriteGuestRowViewModel> Rows { get; set; } = new();
}

public sealed class PartnerFavoriteGuestRowViewModel
{
    public long FavoriteLinkId { get; set; }
    public long UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string MaskedEmail { get; set; } = string.Empty;
    public string UserCity { get; set; } = string.Empty;
    public string SourcePage { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string AddedText { get; set; } = string.Empty;
}

public sealed class PartnerMarketingEventsPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public long SelectedHotelId { get; set; }
    public string IntroNote { get; set; } = string.Empty;
    public List<PartnerStatCardViewModel> SummaryCards { get; set; } = new();
    public List<PartnerCampaignParticipationRowViewModel> Participations { get; set; } = new();
}

public sealed class PartnerCampaignParticipationRowViewModel
{
    public long CampaignHotelLinkId { get; set; }
    public long CampaignId { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public string CampaignCode { get; set; } = string.Empty;
    public string ParticipationStatus { get; set; } = string.Empty;
    public string WindowStartText { get; set; } = string.Empty;
    public string WindowEndText { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public string? LandingUrl { get; set; }
    public string? PartnerNote { get; set; }
}

public sealed class PartnerCampaignParticipationNoteRequest
{
    public long HotelId { get; set; }
    public long CampaignHotelLinkId { get; set; }
    public string? PartnerNote { get; set; }
}

public sealed class PartnerSettingsPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
}

public sealed class PartnerAccountInfoPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public PartnerAccountUserSectionViewModel UserSection { get; set; } = new();
    public PartnerAccountPartnerSectionViewModel? PartnerSection { get; set; }
    public PartnerAccountInfoUpdateForm UpdateForm { get; set; } = new();
}

public sealed class PartnerAccountInfoUpdateForm
{
    public long? HotelId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyType { get; set; } = string.Empty;
    public string TaxOffice { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public string AuthorizedName { get; set; } = string.Empty;
    public string AuthorizedEmail { get; set; } = string.Empty;
    public string AuthorizedPhone { get; set; } = string.Empty;
    public string BillingAddress { get; set; } = string.Empty;
    public string BillingCity { get; set; } = string.Empty;
    public string BillingDistrict { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankBranch { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
}

public sealed class PartnerAccountUserSectionViewModel
{
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string RegistrationText { get; set; } = string.Empty;
    public string LastLoginText { get; set; } = string.Empty;
    public string AccountStatusText { get; set; } = string.Empty;
    public string EmailVerificationText { get; set; } = string.Empty;
    public string PhoneVerificationText { get; set; } = string.Empty;
    public string KvkkText { get; set; } = string.Empty;
    public string MarketingConsentText { get; set; } = string.Empty;
    public string? ProfilePhotoPath { get; set; }
}

public sealed class PartnerAccountPartnerSectionViewModel
{
    public long PartnerId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyType { get; set; } = string.Empty;
    public string TaxOffice { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public string AuthorizedName { get; set; } = string.Empty;
    public string AuthorizedEmail { get; set; } = string.Empty;
    public string AuthorizedPhone { get; set; } = string.Empty;
    public string BillingAddress { get; set; } = string.Empty;
    public string BillingCity { get; set; } = string.Empty;
    public string BillingDistrict { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankBranch { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public string RegistrationText { get; set; } = string.Empty;
    public string? ApprovalDateText { get; set; }
    public string? RejectionReason { get; set; }
    public string? ContractNo { get; set; }
    public string? ContractStartText { get; set; }
    public string? ContractEndText { get; set; }
    public bool PartnerActive { get; set; } = true;
}
