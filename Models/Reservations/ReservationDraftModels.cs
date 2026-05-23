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
    /// <summary>REZERVASYON_TASLAKLARI.MISAFIR_ULKE_ID</summary>
    public long? GuestUlkeId { get; set; }
    /// <summary>REZERVASYON_TASLAKLARI.MISAFIR_IL_ID</summary>
    public long? GuestIlId { get; set; }
    /// <summary>REZERVASYON_TASLAKLARI.MISAFIR_ILCE_ID</summary>
    public long? GuestIlceId { get; set; }
    /// <summary>REZERVASYON_TASLAKLARI.MISAFIR_MAHALLE_ID</summary>
    public long? GuestMahalleId { get; set; }
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int AdultCount { get; set; } = 2;
    public int ChildCount { get; set; }
    public int RoomCount { get; set; } = 1;
    public decimal? NightlyPrice { get; set; }
    public decimal? NetRoomAmount { get; set; }
    public decimal? VatRate { get; set; }
    public decimal? VatAmount { get; set; }
    public decimal? AccommodationTaxRate { get; set; }
    public decimal? AccommodationTaxAmount { get; set; }
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
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public string CheckInText { get; set; } = string.Empty;
    public string CheckOutText { get; set; } = string.Empty;
    public int AdultCount { get; set; }
    public int ChildCount { get; set; }
    public int RoomCount { get; set; }
    public string TotalText { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ResumeUrl { get; set; } = string.Empty;
    public string ProfileCompletionUrl { get; set; } = string.Empty;
    public bool RequiresProfileCompletion { get; set; }
}

public class PublicHotelReservationForm
{
    public long HotelId { get; set; }
    public long RoomTypeId { get; set; }
    public DateOnly CheckInDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly CheckOutDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
    public int AdultCount { get; set; } = 2;
    public int ChildCount { get; set; }
    public int RoomCount { get; set; } = 1;
    public string PaymentMethod { get; set; } = "Kapıda Ödeme";
    public string? RoomsJson { get; set; }

    /// <summary>Kart ile ödenmek istenen tutar (karma planlarda).</summary>
    public decimal CardAmount { get; set; }

    /// <summary>Havale/EFT ile ödenmek istenen tutar.</summary>
    public decimal BankTransferAmount { get; set; }

    /// <summary>Otelde ödenecek tutar (karma).</summary>
    public decimal CashAtHotelAmountSplit { get; set; }

    /// <summary>Havale için referans veya dekont notu.</summary>
    public string? BankTransferReference { get; set; }
}

public sealed class PublicMultiRoomSelectionItem
{
    public long RoomTypeId { get; set; }
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int RoomCount { get; set; } = 1;
}

public class PublicReservationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string RedirectUrl { get; set; } = string.Empty;
    public long? ReservationId { get; set; }
}

public class PublicReservationPriceQuoteViewModel
{
    public int NightCount { get; set; }
    public decimal NightlyPrice { get; set; }
    public decimal RoomTotal { get; set; }
    public decimal NetRoomAmount { get; set; }
    public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal AccommodationTaxRate { get; set; }
    public decimal AccommodationTaxAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string? AvailabilityMessage { get; set; }
    public List<PublicReservationNightlyBreakdownItemViewModel> NightlyBreakdown { get; set; } = new();
}

public class PublicReservationNightlyBreakdownItemViewModel
{
    public DateOnly Date { get; set; }
    public string DateText { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal BasePrice { get; set; }
    public decimal? DiscountPrice { get; set; }
    public string PriceText { get; set; } = string.Empty;
    public bool IsDiscounted { get; set; }
    public bool IsClosed { get; set; }
    public short RemainingRooms { get; set; }
}
