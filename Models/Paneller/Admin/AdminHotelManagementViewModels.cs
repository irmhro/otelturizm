using Microsoft.AspNetCore.Http;

namespace otelturizmnew.Models.Paneller.Admin;

public class AdminHotelsPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
    public string CityFilter { get; set; } = string.Empty;
    public string DistrictFilter { get; set; } = string.Empty;
    public string NeighborhoodFilter { get; set; } = string.Empty;
    public string PublishStatusFilter { get; set; } = string.Empty;
    public string ApprovalStatusFilter { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalCount { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)Math.Max(PageSize, 1)));
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
    public List<string> CityOptions { get; set; } = new();
    public List<string> DistrictOptions { get; set; } = new();
    public List<string> NeighborhoodOptions { get; set; } = new();
    public List<string> PublishStatusOptions { get; set; } = new();
    public List<string> ApprovalStatusOptions { get; set; } = new();
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
    public bool IsPublished { get; set; }
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
    public List<AdminHotelMissingFieldViewModel> MissingFields { get; set; } = new();
    public List<AdminHotelAmenityViewModel> Amenities { get; set; } = new();
    public List<AdminHotelPriceRowViewModel> PriceRows { get; set; } = new();
    public List<AdminHotelDocumentViewModel> Documents { get; set; } = new();
    public AdminHotelManagerViewModel ManagerInfo { get; set; } = new();
    public List<AdminHotelPartnerUserViewModel> PartnerUsers { get; set; } = new();
    public List<AdminHotelEmailAccountViewModel> EmailAccounts { get; set; } = new();
    public AdminHotelReservationStatsViewModel ReservationStats { get; set; } = new();
    public List<AdminHotelReservationRowViewModel> RecentReservations { get; set; } = new();
    public AdminHotelCheckInOutViewModel CheckInOutRules { get; set; } = new();
    public AdminHotelCommissionSummaryViewModel CommissionSummary { get; set; } = new();
    public List<AdminHotelRoomTableRowViewModel> RoomTableRows { get; set; } = new();
    public int CompletenessScore { get; set; }
    public int CompletenessTotalRules { get; set; }
    public int CompletenessCompletedRules { get; set; }
    public int CompletenessCriticalMissingCount { get; set; }
    public List<AdminHotelCompletenessRuleViewModel> CompletenessRules { get; set; } = new();
}

public class AdminHotelCompletenessRuleViewModel
{
    public string FieldKey { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string Severity { get; set; } = "warning";
    public string TabTarget { get; set; } = string.Empty;
    public string? PartnerFixUrl { get; set; }
    public bool IsMissing { get; set; }
}

public class AdminHotelManagerViewModel
{
    public string CompanyName { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    public string ManagerPhone { get; set; } = string.Empty;
    public string ManagerEmail { get; set; } = string.Empty;
    public string ManagerRole { get; set; } = string.Empty;
    public string SalesContactName { get; set; } = string.Empty;
    public string SalesContactPhone { get; set; } = string.Empty;
    public string SalesContactEmail { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class AdminHotelPartnerUserViewModel
{
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsMainResponsible { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LinkedAt { get; set; }
}

public class AdminHotelEmailAccountViewModel
{
    public string Source { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDefaultSender { get; set; }
    public DateTime? LastSyncAt { get; set; }
}

public class AdminHotelReservationStatsViewModel
{
    public int TotalReservations { get; set; }
    public int UniqueGuestCount { get; set; }
    public int ConfirmedCount { get; set; }
    public int CancelledCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal AverageNightlyRate { get; set; }
}

public class AdminHotelReservationRowViewModel
{
    public long ReservationId { get; set; }
    public string ReservationNo { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string GuestEmail { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal CommissionRate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class AdminHotelCheckInOutViewModel
{
    public string CheckInTime { get; set; } = string.Empty;
    public string CheckOutTime { get; set; } = string.Empty;
    public bool LateCheckoutAvailable { get; set; }
    public decimal? LateCheckoutFee { get; set; }
    public bool EarlyCheckInAvailable { get; set; }
    public decimal? EarlyCheckInFee { get; set; }
    public int? MinStay { get; set; }
    public int? MaxStay { get; set; }
    public string? CancellationPolicySummary { get; set; }
    public string? DetailedCancellationRules { get; set; }
    public byte? FreeCancellationHours { get; set; }
    public decimal? NoShowPenaltyRate { get; set; }
    public string? QuietHours { get; set; }
    public string? SpecialRules { get; set; }
}

public class AdminHotelCommissionSummaryViewModel
{
    public string CommissionType { get; set; } = string.Empty;
    public decimal DefaultCommissionRate { get; set; }
    public string CommissionCalculationType { get; set; } = string.Empty;
    public string PaymentTerm { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string InvoiceType { get; set; } = string.Empty;
    public decimal TotalReservationAmount { get; set; }
    public decimal TotalCommissionAmount { get; set; }
    public decimal AverageRoomBasePrice { get; set; }
}

public class AdminHotelRoomTableRowViewModel
{
    public long RoomId { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public int MaxPeople { get; set; }
    public int TotalRooms { get; set; }
    public string BedType { get; set; } = string.Empty;
    public int? RoomSize { get; set; }
    public bool IsActive { get; set; }
}

public class AdminHotelMissingFieldViewModel
{
    public string FieldKey { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string Severity { get; set; } = "warning";
    public string TabTarget { get; set; } = string.Empty;
    public string? PartnerFixUrl { get; set; }
    public bool IsComplete { get; set; }
}

public class AdminHotelAmenityViewModel
{
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class AdminHotelPriceRowViewModel
{
    public string RoomName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Price { get; set; }
    public int? AvailableRooms { get; set; }
}

public class AdminHotelDocumentViewModel
{
    public long DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? UploadedAt { get; set; }
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
    public long? UlkeId { get; set; }
    public long? SehirId { get; set; }
    public long? IlceId { get; set; }
    public long? MahalleId { get; set; }
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
