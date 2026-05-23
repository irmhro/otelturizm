using System.ComponentModel.DataAnnotations;

namespace otelturizmnew.Models.Paneller.Admin;

public sealed class AdminActionLogFilter
{
    public long? AdminUserId { get; set; }
    public string? ActionType { get; set; }
    public string? TargetTable { get; set; }
    public string? Query { get; set; }
    public DateTimeOffset? FromUtc { get; set; }
    public DateTimeOffset? ToUtc { get; set; }
    public string? Sort { get; set; } // date_desc, date_asc
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public sealed class AdminActionLogRowViewModel
{
    public long Id { get; set; }
    public long AdminUserId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string TargetTable { get; set; } = string.Empty;
    public string? TargetId { get; set; }
    public string? Note { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class AdminActionLogsPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public AdminActionLogFilter Filter { get; set; } = new();
    public List<AdminActionLogRowViewModel> Rows { get; set; } = new();
    public int Total { get; set; }
}

public sealed class AdminEmailQueueFilter
{
    public string? Status { get; set; } // Beklemede/Gonderildi/Basarısız
    public string? Query { get; set; } // email/subject
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public sealed class AdminEmailQueueRowViewModel
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ProviderMessageId { get; set; }
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? NextAttemptUtc { get; set; }
    public string? LastError { get; set; }
}

public sealed class AdminEmailQueuePageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public AdminEmailQueueFilter Filter { get; set; } = new();
    public List<AdminEmailQueueRowViewModel> Rows { get; set; } = new();
    public int Total { get; set; }
}

public sealed class AdminEmailAccountRowViewModel
{
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string? ReplyToEmail { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string SecurityType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public bool TestMode { get; set; }
    public DateTimeOffset? LastSuccessUtc { get; set; }
    public DateTimeOffset? LastErrorUtc { get; set; }
    public string? LastErrorMessage { get; set; }
}

public sealed class AdminEmailTemplateBindingRowViewModel
{
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string Language { get; set; } = "tr";
    public string Subject { get; set; } = string.Empty;
    public string ViewPath { get; set; } = string.Empty;
    public string TriggerArea { get; set; } = string.Empty;
    public string IntendedSenderEmail { get; set; } = string.Empty;
    public string ActualSenderEmail { get; set; } = string.Empty;
    public bool UsesFallbackSender { get; set; }
}

public sealed class AdminEmailSettingsPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminEmailAccountRowViewModel> Accounts { get; set; } = new();
    public List<AdminEmailTemplateBindingRowViewModel> Templates { get; set; } = new();
    public int PendingCount { get; set; }
    public int AcceptedCount { get; set; }
    public int FailedCount { get; set; }
    public string ActiveSenderEmail { get; set; } = string.Empty;
    public string ActiveServiceCode { get; set; } = string.Empty;
}

public sealed class AdminMailAccountForm
{
    public long? Id { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string IncomingProtocol { get; set; } = "IMAP";
    public string IncomingHost { get; set; } = "mail.otelturizm.com";
    public int IncomingPort { get; set; } = 993;
    public bool IncomingUseSsl { get; set; } = true;
    public string OutgoingHost { get; set; } = "mail.otelturizm.com";
    public int OutgoingPort { get; set; } = 465;
    public string OutgoingSecurityType { get; set; } = "SSL/TLS";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsDefaultSender { get; set; }
}

public sealed class AdminMailAccountRowViewModel
{
    public long Id { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string IncomingProtocol { get; set; } = "IMAP";
    public string IncomingHost { get; set; } = string.Empty;
    public int IncomingPort { get; set; }
    public bool IncomingUseSsl { get; set; }
    public string OutgoingHost { get; set; } = string.Empty;
    public int OutgoingPort { get; set; }
    public string OutgoingSecurityType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDefaultSender { get; set; }
    public DateTimeOffset? LastSyncUtc { get; set; }
    public string? LastError { get; set; }
}

public sealed class AdminIncomingEmailRowViewModel
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public string AccountEmail { get; set; } = string.Empty;
    public string FolderName { get; set; } = "INBOX";
    public string From { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? InternetMessageId { get; set; }
    public DateTimeOffset? ReceivedAtUtc { get; set; }
    public bool IsRead { get; set; }
    public bool IsSpam { get; set; }
}

public sealed class AdminOutgoingEmailRowViewModel
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ProviderMessageId { get; set; }
    public string? SenderEmail { get; set; }
    public DateTimeOffset? SentAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class AdminMailCenterPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminMailAccountRowViewModel> Accounts { get; set; } = new();
    public List<AdminIncomingEmailRowViewModel> Incoming { get; set; } = new();
    public List<AdminOutgoingEmailRowViewModel> Outgoing { get; set; } = new();
    public AdminMailAccountForm Form { get; set; } = new();
    public long? SelectedAccountId { get; set; }
    public int TotalIncoming { get; set; }
    public int TotalOutgoing { get; set; }
    public string ActiveSenderEmail { get; set; } = string.Empty;
}

public sealed class AdminCriticalActionRequest
{
    [Required(ErrorMessage = "Gerekçe zorunludur.")]
    [StringLength(240, MinimumLength = 5, ErrorMessage = "Gerekçe 5-240 karakter olmalı.")]
    public string Reason { get; set; } = string.Empty;
}

public sealed class AdminUnifiedReservationRowViewModel
{
    public long ReservationId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CompanyName { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string SourceText { get; set; } = "Bireysel";
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class AdminUnifiedReservationsPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public string? Query { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int Total { get; set; }
    public List<AdminUnifiedReservationRowViewModel> Rows { get; set; } = new();
}

public sealed class AdminPaymentRowViewModel
{
    public long PaymentId { get; set; }
    public string TransactionNo { get; set; } = string.Empty;
    public string ReservationNo { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalCollected { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Provider { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public bool ManualApprovalRequired { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
}

public sealed class AdminPaymentsPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminSummaryCardViewModel> SummaryCards { get; set; } = new();
    public string? Query { get; set; }
    public string? Status { get; set; }
    public string? PaymentType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int Total { get; set; }
    public List<string> StatusOptions { get; set; } = new();
    public List<string> TypeOptions { get; set; } = new();
    public List<AdminPaymentRowViewModel> Rows { get; set; } = new();
}

public sealed class AdminInvoiceRowViewModel
{
    public long InvoiceId { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public string InvoiceType { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string BuyerTitle { get; set; } = string.Empty;
    public string BuyerEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string EInvoiceStatus { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal AccommodationTaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string PdfPath { get; set; } = string.Empty;
}

public sealed class AdminInvoicesPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminSummaryCardViewModel> SummaryCards { get; set; } = new();
    public string? Query { get; set; }
    public string? Status { get; set; }
    public string? InvoiceType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int Total { get; set; }
    public List<string> StatusOptions { get; set; } = new();
    public List<string> TypeOptions { get; set; } = new();
    public List<AdminInvoiceRowViewModel> Rows { get; set; } = new();
}

public sealed class AdminReportHotelOptionViewModel
{
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
}

public sealed class AdminRevenueReportRowViewModel
{
    public string MonthText { get; set; } = string.Empty;
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string CityLabel { get; set; } = string.Empty;
    public int ReservationCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal GrossCommission { get; set; }
    public decimal NetCommission { get; set; }
    public decimal AccommodationTax { get; set; }
    public decimal KdvAmount { get; set; }
    public decimal AverageNightAmount => ReservationCount <= 0 ? 0m : GrossRevenue / ReservationCount;
}

public sealed class AdminReportsPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminSummaryCardViewModel> SummaryCards { get; set; } = new();
    public List<AdminReportHotelOptionViewModel> HotelOptions { get; set; } = new();
    public long? HotelId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int Total { get; set; }
    public List<AdminRevenueReportRowViewModel> Rows { get; set; } = new();
}

public sealed class AdminRevenueCommandCenterPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminSummaryCardViewModel> SummaryCards { get; set; } = new();
    public List<AdminChartBarViewModel> GmvDailyTrend { get; set; } = new();
    public List<AdminChartBarViewModel> ReservationDailyTrend { get; set; } = new();
    public List<AdminRevenueHotelLeaderRowViewModel> TopHotels { get; set; } = new();
    public bool PackageTablesReady { get; set; }
    public int PackageApplications30d { get; set; }
    public int Package5651Applications30d { get; set; }
    public int Package5661Applications30d { get; set; }
}

public sealed class AdminRevenueHotelLeaderRowViewModel
{
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string CityLabel { get; set; } = string.Empty;
    public int ReservationCount { get; set; }
    public int CancelledCount { get; set; }
    public decimal Gmv { get; set; }
    public decimal Commission { get; set; }
}

public sealed class AdminCampaignRowViewModel
{
    public long CampaignId { get; set; }
    public string CampaignCode { get; set; } = string.Empty;
    public string CampaignName { get; set; } = string.Empty;
    public string CampaignType { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public decimal DiscountRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal MaxDiscountAmount { get; set; }
    public decimal MinCartAmount { get; set; }
    public DateTimeOffset? StartsAtUtc { get; set; }
    public DateTimeOffset? EndsAtUtc { get; set; }
    public bool IsActive { get; set; }
    public string VisibilityStatus { get; set; } = string.Empty;
    public bool PartnerJoinOpen { get; set; }
    public int UsedCount { get; set; }
    public int? TotalLimit { get; set; }
    public bool Featured { get; set; }
    public bool ShowcaseActive { get; set; }
    public string TargetCities { get; set; } = string.Empty;
    public string Badge { get; set; } = string.Empty;
    public string ColorCode { get; set; } = string.Empty;
    public int RemainingLimit => TotalLimit.HasValue ? Math.Max(0, TotalLimit.Value - UsedCount) : 0;
}

public sealed class AdminCampaignsPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminSummaryCardViewModel> SummaryCards { get; set; } = new();
    public string? Query { get; set; }
    public string? Status { get; set; }
    public string? CampaignType { get; set; }
    public bool? PartnerJoinOpen { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int Total { get; set; }
    public List<string> StatusOptions { get; set; } = new();
    public List<string> TypeOptions { get; set; } = new();
    public List<AdminCampaignRowViewModel> Rows { get; set; } = new();
}

public sealed class AdminNotificationLogRowViewModel
{
    public long NotificationId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ServiceCode { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public int MaxAttemptCount { get; set; }
    public DateTimeOffset? CreatedAtUtc { get; set; }
    public DateTimeOffset? SentAtUtc { get; set; }
    public DateTimeOffset? ReadAtUtc { get; set; }
    public string RelatedTable { get; set; } = string.Empty;
    public long? RelatedId { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public sealed class AdminNotificationsPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminSummaryCardViewModel> SummaryCards { get; set; } = new();
    public string? Query { get; set; }
    public string? Status { get; set; }
    public string? NotificationType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int Total { get; set; }
    public List<string> StatusOptions { get; set; } = new();
    public List<string> TypeOptions { get; set; } = new();
    public List<AdminNotificationLogRowViewModel> Rows { get; set; } = new();
}

public sealed class AdminRateLimitEndpointStatViewModel
{
    public string Endpoint { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int Count429 { get; set; }
    public int CountTotal { get; set; }
}

public sealed class AdminRateLimitStatsPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public int WindowHours { get; set; } = 24;
    public List<AdminRateLimitEndpointStatViewModel> Rows { get; set; } = new();
}

public sealed class AdminLogEventRowViewModel
{
    public DateTimeOffset? Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Raw { get; set; } = string.Empty;
}

public sealed class AdminLogEventsPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public string EventType { get; set; } = "SECURITY_EVENT";
    public int Take { get; set; } = 200;
    public List<AdminLogEventRowViewModel> Rows { get; set; } = new();
    public string? Warning { get; set; }
}

public sealed class AdminSlowSqlRowViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public long Count { get; set; }
    public long MaxMs { get; set; }
    public double AvgMs { get; set; }
    public DateTimeOffset LastSeenUtc { get; set; }
    public string SampleSql { get; set; } = string.Empty;
}

public sealed class AdminSlowSqlPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public int Take { get; set; } = 50;
    public List<AdminSlowSqlRowViewModel> Rows { get; set; } = new();
}

public sealed class AdminSettingsMonitorPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public Dictionary<string, string?> Items { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class AdminSecurityLoginRowViewModel
{
    public long LoginId { get; set; }
    public long UserId { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string DeviceLabel { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTimeOffset? LoginAtUtc { get; set; }
}

public sealed class AdminSecurityApiRiskRowViewModel
{
    public long LogId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public bool Success { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTimeOffset? StartedAtUtc { get; set; }
}

public sealed class AdminSecurityPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminSummaryCardViewModel> SummaryCards { get; set; } = new();
    public List<AdminSecurityLoginRowViewModel> RecentLogins { get; set; } = new();
    public List<AdminSecurityApiRiskRowViewModel> ApiRisks { get; set; } = new();
}

public sealed class AdminPlatformCheckupPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminSummaryCardViewModel> SummaryCards { get; set; } = new();
    public List<AdminPlatformCheckupGroupViewModel> Groups { get; set; } = new();
    public List<AdminPlatformRoadmapItemViewModel> Roadmap { get; set; } = new();
}

public sealed class AdminPlatformCheckupGroupViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ToneClass { get; set; } = "info";
    public List<AdminPlatformCheckupItemViewModel> Items { get; set; } = new();
}

public sealed class AdminPlatformCheckupItemViewModel
{
    public string Label { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string ToneClass { get; set; } = "info";
    public string? ActionUrl { get; set; }
}

public sealed class AdminPlatformRoadmapItemViewModel
{
    public string Phase { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}

public sealed class AdminCommerceRumRowViewModel
{
    public string RouteMetric { get; set; } = string.Empty;
    public double Avg { get; set; }
    public int Count { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
}

public sealed class AdminCommerceInventoryRowViewModel
{
    public string HotelName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public int Remaining { get; set; }
}

public sealed class AdminCommercePriceRowViewModel
{
    public string RoomName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public decimal BasePrice { get; set; }
    public decimal? DiscountPrice { get; set; }
}

public sealed class AdminCommerceInsightPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public bool KillSwitchConfig { get; set; }
    public bool KillSwitchEmergency { get; set; }
    public int ReservationsLast7Days { get; set; }
    public decimal RevenueLast7Days { get; set; }
    public List<AdminCommerceRumRowViewModel> RumRows { get; set; } = new();
    public Dictionary<string, int> GrowthKinds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<AdminCommerceInventoryRowViewModel> InventoryRows { get; set; } = new();
    public List<AdminCommercePriceRowViewModel> PriceSampleRows { get; set; } = new();
    public long PriceHistoryHotelId { get; set; }
    public int ArchivedReservationSampleCount { get; set; }
    public string? ArchiveHint { get; set; }
}

