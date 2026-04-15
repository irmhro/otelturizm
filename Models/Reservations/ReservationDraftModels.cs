namespace otelturizmnew.Models.Reservations;

public class ReservationDraftUpsertRequest
{
    public long? UserId { get; set; }
    public string? SessionKey { get; set; }
    public string Source { get; set; } = "Public";
    public string Status { get; set; } = "Taslak";
    public long HotelId { get; set; }
    public long? RoomTypeId { get; set; }
    public string? GuestFullName { get; set; }
    public string? GuestEmail { get; set; }
    public string? GuestPhone { get; set; }
    public string? GuestCity { get; set; }
    public string? GuestDistrict { get; set; }
    public string? GuestNeighborhood { get; set; }
    public string? GuestAddress { get; set; }
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int AdultCount { get; set; } = 2;
    public int ChildCount { get; set; }
    public int RoomCount { get; set; } = 1;
    public decimal? NightlyPrice { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? TotalAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? ReturnUrl { get; set; }
    public string? ProfileCompletionUrl { get; set; }
    public string? Notes { get; set; }
}

public class ReservationDraftSummaryViewModel
{
    public long DraftId { get; set; }
    public long HotelId { get; set; }
    public long? RoomTypeId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CheckInText { get; set; } = string.Empty;
    public string CheckOutText { get; set; } = string.Empty;
    public int AdultCount { get; set; }
    public int ChildCount { get; set; }
    public int RoomCount { get; set; }
    public string TotalText { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ResumeUrl { get; set; } = string.Empty;
}

public class PublicHotelReservationForm
{
    public long HotelId { get; set; }
    public long RoomTypeId { get; set; }
    public DateOnly CheckInDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
    public DateOnly CheckOutDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
    public int AdultCount { get; set; } = 2;
    public int ChildCount { get; set; }
    public int RoomCount { get; set; } = 1;
}

public class PublicReservationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string RedirectUrl { get; set; } = string.Empty;
    public long? ReservationId { get; set; }
}
