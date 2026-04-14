using Microsoft.AspNetCore.Http;

namespace otelturizmnew.Models.Paneller.Admin;

public class AdminHotelsPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
    public List<AdminSummaryCardViewModel> SummaryCards { get; set; } = new();
    public List<AdminHotelListItemViewModel> Hotels { get; set; } = new();
}

public class AdminHotelListItemViewModel
{
    public long HotelId { get; set; }
    public string HotelCode { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string HotelType { get; set; } = string.Empty;
    public string LocationText { get; set; } = string.Empty;
    public string PublishStatus { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public string ScoreText { get; set; } = "0.0";
    public int RoomCount { get; set; }
    public int HotelPhotoCount { get; set; }
    public int RoomPhotoCount { get; set; }
    public bool IsFeatured { get; set; }
}

public class AdminHotelManagementPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminSummaryCardViewModel> SummaryCards { get; set; } = new();
    public AdminHotelEditForm HotelForm { get; set; } = new();
    public List<AdminRoomCardViewModel> Rooms { get; set; } = new();
    public AdminRoomEditForm RoomForm { get; set; } = new();
    public bool IsEditingRoom => RoomForm.RoomId.HasValue;
    public List<AdminPhotoCardViewModel> HotelPhotos { get; set; } = new();
    public AdminHotelPhotoUploadForm HotelPhotoUploadForm { get; set; } = new();
    public AdminHotelPhotoEditForm HotelPhotoEditForm { get; set; } = new();
    public bool IsEditingHotelPhoto => HotelPhotoEditForm.PhotoId.HasValue;
    public List<AdminRoomPhotoCardViewModel> RoomPhotos { get; set; } = new();
    public AdminRoomPhotoUploadForm RoomPhotoUploadForm { get; set; } = new();
    public AdminRoomPhotoEditForm RoomPhotoEditForm { get; set; } = new();
    public bool IsEditingRoomPhoto => RoomPhotoEditForm.PhotoId.HasValue;
    public long? SelectedRoomId { get; set; }
    public string SelectedRoomName { get; set; } = string.Empty;
}

public class AdminHotelEditForm
{
    public long HotelId { get; set; }
    public string HotelCode { get; set; } = string.Empty;
    public long PartnerId { get; set; }
    public long? UserId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string HotelType { get; set; } = "Otel";
    public int? StarCount { get; set; }
    public string? TourismDocumentNo { get; set; }
    public string? TourismDocumentType { get; set; }
    public string Country { get; set; } = "Türkiye";
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string? Neighborhood { get; set; }
    public string Address { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string Phone1 { get; set; } = string.Empty;
    public string? Phone2 { get; set; }
    public string? Fax { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? ReservationPhone { get; set; }
    public string? SalesContactName { get; set; }
    public string? SalesContactPhone { get; set; }
    public string? SalesContactEmail { get; set; }
    public string? SalesNotes { get; set; }
    public string? CheckInTime { get; set; }
    public string? CheckOutTime { get; set; }
    public bool LateCheckoutAvailable { get; set; }
    public decimal? LateCheckoutFee { get; set; }
    public bool EarlyCheckInAvailable { get; set; }
    public decimal? EarlyCheckInFee { get; set; }
    public int TotalRoomCount { get; set; }
    public int? TotalBedCapacity { get; set; }
    public int? FloorCount { get; set; }
    public bool ElevatorAvailable { get; set; }
    public int? ElevatorCount { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? LocationDescription { get; set; }
    public string CommissionType { get; set; } = "sabit_oran";
    public decimal DefaultCommissionRate { get; set; }
    public string CommissionCalculationType { get; set; } = "toplam_tutar_uzerinden";
    public string PaymentTerm { get; set; } = "Çıkış Günü";
    public string PaymentMethod { get; set; } = "Havale/EFT";
    public string InvoiceType { get; set; } = "Otel Keser";
    public decimal? DepositAmount { get; set; }
    public int? DepositReturnDays { get; set; }
    public int? MinStay { get; set; }
    public int? MaxStay { get; set; }
    public string? SpokenLanguages { get; set; }
    public decimal? AverageScore { get; set; }
    public int? TotalReviewCount { get; set; }
    public decimal? CleanlinessScore { get; set; }
    public decimal? ComfortScore { get; set; }
    public decimal? LocationScore { get; set; }
    public decimal? StaffScore { get; set; }
    public decimal? PricePerformanceScore { get; set; }
    public string? CoverPhotoPath { get; set; }
    public string? VideoUrl { get; set; }
    public string? VirtualTourUrl { get; set; }
    public string PublishStatus { get; set; } = "Taslak";
    public string ApprovalStatus { get; set; } = "Beklemede";
    public int PopularityOrder { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsRecommended { get; set; }
}

public class AdminRoomCardViewModel
{
    public long RoomId { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string PriceText { get; set; } = string.Empty;
    public string CapacityText { get; set; } = string.Empty;
    public string StockText { get; set; } = string.Empty;
    public string CoverPhotoUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class AdminRoomEditForm
{
    public long HotelId { get; set; }
    public long? RoomId { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string RoomCategory { get; set; } = "Standart";
    public int MaxPeople { get; set; } = 2;
    public int MaxAdults { get; set; } = 2;
    public int MaxChildren { get; set; }
    public string? BedType { get; set; }
    public int? BedCount { get; set; }
    public bool ExtraBedAvailable { get; set; }
    public int? RoomSize { get; set; }
    public bool BalconyAvailable { get; set; }
    public int? BalconySize { get; set; }
    public string? ViewType { get; set; }
    public bool PrivateBathroom { get; set; } = true;
    public string? BathroomType { get; set; }
    public decimal BasePrice { get; set; }
    public decimal? WeekendDifferenceRate { get; set; }
    public decimal? ChildDiscountRate { get; set; }
    public bool BabyFree { get; set; } = true;
    public int? BabyAgeLimit { get; set; }
    public int? ChildAgeLimit { get; set; }
    public int TotalRooms { get; set; } = 1;
    public int? OverbookingLimit { get; set; }
    public string? CoverPhotoPath { get; set; }
    public string? GalleryJson { get; set; }
    public string? FeaturesText { get; set; }
    public bool IsActive { get; set; } = true;
    public int? SortOrder { get; set; }
}

public class AdminPhotoCardViewModel
{
    public long PhotoId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsCover { get; set; }
    public bool IsApproved { get; set; }
}

public class AdminRoomPhotoCardViewModel
{
    public long PhotoId { get; set; }
    public long RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsCover { get; set; }
    public bool IsApproved { get; set; }
}

public class AdminHotelPhotoUploadForm
{
    public long HotelId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PhotoType { get; set; } = "Genel Alan";
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool MakeCover { get; set; }
    public List<IFormFile>? Files { get; set; }
}

public class AdminHotelPhotoEditForm
{
    public long HotelId { get; set; }
    public long? PhotoId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PhotoType { get; set; } = "Genel Alan";
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool MarkAsFeatured { get; set; }
}

public class AdminRoomPhotoUploadForm
{
    public long HotelId { get; set; }
    public long RoomId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool MakeCover { get; set; }
    public List<IFormFile>? Files { get; set; }
}

public class AdminRoomPhotoEditForm
{
    public long HotelId { get; set; }
    public long RoomId { get; set; }
    public long? PhotoId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
