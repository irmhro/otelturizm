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
    public string Currency { get; set; } = "TRY";
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

public sealed class AdminSettingsMonitorPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public Dictionary<string, string?> Items { get; set; } = new(StringComparer.OrdinalIgnoreCase);
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

