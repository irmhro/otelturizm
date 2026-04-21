using otelturizmnew.Models.Messages;
using otelturizmnew.Models.TelefonDogrulama;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Models.Paneller.User;

public class UserDashboardPageViewModel
{
    public int TotalReservationCount { get; set; }
    public int UpcomingReservationCount { get; set; }
    public int FavoriteCount { get; set; }
    public int MessageCount { get; set; }
    public string DiscountText { get; set; } = "₺0";
    public List<UserReservationCardViewModel> RecentReservations { get; set; } = new();
    public List<UserFavoriteSummaryViewModel> FavoriteHotels { get; set; } = new();
    public string ReservationStatusFilter { get; set; } = "all";
    public string? ReservationStartDateText { get; set; }
    public string? ReservationEndDateText { get; set; }
    public int ReservationPage { get; set; } = 1;
    public int ReservationPageSize { get; set; } = 5;
    public int ReservationTotalCount { get; set; }
    public int ReservationTotalPages => ReservationPageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(ReservationTotalCount / (double)ReservationPageSize));
}

public class UserReservationsPageViewModel
{
    public int UpcomingCount { get; set; }
    public int PastCount { get; set; }
    public int CancelledCount { get; set; }
    public int TotalCount { get; set; }
    public string StatusFilter { get; set; } = "all";
    public string? StartDateText { get; set; }
    public string? EndDateText { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 5;
    public int FilteredCount { get; set; }
    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(FilteredCount / (double)PageSize));
    public List<UserReservationCardViewModel> Reservations { get; set; } = new();
}

public class UserReservationCardViewModel
{
    public long ReservationId { get; set; }
    public string ReservationNo { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string HotelSlug { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string StayDateText { get; set; } = string.Empty;
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public string GuestText { get; set; } = string.Empty;
    public string MealOrRoomText { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string StatusTone { get; set; } = "ok";
    public string SubNote { get; set; } = string.Empty;
    public string SubNoteTone { get; set; } = "info";
    public string TotalText { get; set; } = string.Empty;
    public bool CanCancel { get; set; }
    public bool IsUpcoming { get; set; }
    public bool IsCancelled { get; set; }
    public string? CancellationReason { get; set; }
    public string? CancellationTimeText { get; set; }
}

public class UserReservationCancelForm
{
    public long ReservationId { get; set; }
    public string CancellationReason { get; set; } = string.Empty;
}

public class UserFavoriteSummaryViewModel
{
    public string HotelName { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string RatingText { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class UserMessagesPageViewModel
{
    public List<MessageCenterThreadViewModel> Threads { get; set; } = new();
    public long? SelectedConversationId { get; set; }
    public string SelectedTitle { get; set; } = "Mesajlarım";
    public string SelectedSubtitle { get; set; } = "Mesaj detayları";
    public List<MessageCenterItemViewModel> Messages { get; set; } = new();
}

public class UserLoyaltyPageViewModel
{
    public string UserDisplayName { get; set; } = "Misafir";
    public string CurrentTierName { get; set; } = "Bronz";
    public string CurrentTierCode { get; set; } = "B";
    public string CurrentTierSummary { get; set; } = "Sadakat hesabınız aktif.";
    public string? NextTierName { get; set; }
    public string CurrentTierIconClass { get; set; } = "fas fa-crown";
    public string CurrentTierCssClass { get; set; } = "bronze";
    public int TotalPoints { get; set; }
    public int AvailablePoints { get; set; }
    public int PointsToNextTier { get; set; }
    public int ProgressPercent { get; set; }
    public int CurrentYearEarnedPoints { get; set; }
    public int CurrentYearSpentPoints { get; set; }
    public string PointsExpiryText { get; set; } = "Henüz tanımlanmadı";
    public List<UserLoyaltyTierViewModel> Tiers { get; set; } = new();
    public List<UserLoyaltyBenefitViewModel> Benefits { get; set; } = new();
    public List<UserLoyaltyPointTransactionViewModel> PointTransactions { get; set; } = new();
    public List<UserLoyaltyRewardViewModel> Rewards { get; set; } = new();
    public List<UserLoyaltyPriceAlertViewModel> PriceAlerts { get; set; } = new();
    public List<UserLoyaltyBadgeViewModel> Badges { get; set; } = new();
    public List<UserLoyaltyPassportCityViewModel> PassportCities { get; set; } = new();
    public List<UserLoyaltyTravelPlanViewModel> TravelPlans { get; set; } = new();
    public List<UserLoyaltyOfferViewModel> Offers { get; set; } = new();
    public List<UserLoyaltyBudgetPlanViewModel> BudgetPlans { get; set; } = new();
    public List<UserLoyaltyRecommendationViewModel> Recommendations { get; set; } = new();
    public UserLoyaltyBudgetPlanForm BudgetPlanForm { get; set; } = new();
    public UserLoyaltyTravelPlanForm TravelPlanForm { get; set; } = new();
}

public class UserLoyaltyTierViewModel
{
    public long TierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int MinimumPoints { get; set; }
    public int? MaximumPoints { get; set; }
    public string RangeText { get; set; } = string.Empty;
    public string BenefitSummary { get; set; } = string.Empty;
    public string CssClass { get; set; } = "bronze";
    public bool IsCurrent { get; set; }
}

public class UserLoyaltyBenefitViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsUnlocked { get; set; }
    public string IconClass { get; set; } = "fas fa-check-circle";
    public string Tone { get; set; } = "primary";
    public string UnlockText { get; set; } = string.Empty;
}

public class UserLoyaltyPointTransactionViewModel
{
    public string DateText { get; set; } = string.Empty;
    public string TypeText { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PointChange { get; set; }
    public string PointChangeText { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string StatusTone { get; set; } = "completed";
}

public class UserLoyaltyRewardViewModel
{
    public long RewardId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int RequiredPoints { get; set; }
    public bool IsAvailable { get; set; }
    public string IconClass { get; set; } = "fas fa-gift";
    public string Tone { get; set; } = "primary";
}

public class UserLoyaltyPriceAlertViewModel
{
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string CurrentPriceText { get; set; } = string.Empty;
    public string TargetPriceText { get; set; } = string.Empty;
    public bool IsTriggered { get; set; }
}

public class UserLoyaltyBadgeViewModel
{
    public string Title { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fas fa-award";
    public bool IsEarned { get; set; }
    public string ProgressText { get; set; } = string.Empty;
}

public class UserLoyaltyPassportCityViewModel
{
    public string CityName { get; set; } = string.Empty;
    public string CountryName { get; set; } = "Türkiye";
    public bool IsVisited { get; set; }
    public string VisitText { get; set; } = string.Empty;
}

public class UserLoyaltyTravelPlanViewModel
{
    public long PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string DestinationText { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public string BudgetText { get; set; } = string.Empty;
    public string VoteSummary { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
}

public class UserLoyaltyOfferViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string ValidityText { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = "/oteller";
}

public class UserLoyaltyBudgetPlanViewModel
{
    public string DestinationText { get; set; } = string.Empty;
    public string BudgetText { get; set; } = string.Empty;
    public string TravelerText { get; set; } = string.Empty;
    public string SuggestionText { get; set; } = string.Empty;
}

public class UserLoyaltyRecommendationViewModel
{
    public string HotelName { get; set; } = string.Empty;
    public string DistrictText { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string RatingText { get; set; } = string.Empty;
    public string Url { get; set; } = "/oteller";
}

public class UserLoyaltyBudgetPlanForm
{
    public string DestinationCity { get; set; } = string.Empty;
    public decimal? TargetBudget { get; set; }
    public int NightCount { get; set; } = 2;
    public int TravelerCount { get; set; } = 2;
}

public class UserLoyaltyTravelPlanForm
{
    public string PlanName { get; set; } = string.Empty;
    public string DestinationCity { get; set; } = string.Empty;
    public string? StartDateText { get; set; }
    public string? EndDateText { get; set; }
    public decimal? BudgetAmount { get; set; }
}

public class UserProfilePageViewModel
{
    public UserProfileForm Form { get; set; } = new();
    public UserPhoneVerificationStatusViewModel PhoneVerification { get; set; } = new();
    public List<AddressCountryOption> Countries { get; set; } = new();
    public List<AddressProvinceOption> Provinces { get; set; } = new();
    public long? SelectedCountryId { get; set; }
    public long? SelectedProvinceId { get; set; }
    public long? SelectedDistrictId { get; set; }
    public long? SelectedNeighborhoodId { get; set; }
    public bool OpenCompletionModal { get; set; }
    public bool OpenPhoneVerification { get; set; }
    public string ReturnUrl { get; set; } = string.Empty;
}

public class UserProfileForm
{
    public string? ReturnUrl { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? IdentityNumber { get; set; }
    public string? BirthDateText { get; set; }
    public string? Gender { get; set; }
    public string? Nationality { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Neighborhood { get; set; }
    public string? PostalCode { get; set; }
    public string? RoomPreference { get; set; }
    public string? BedPreference { get; set; }
    public string? SpokenLanguages { get; set; }
    public string? TravelPurpose { get; set; }
    public string? SpecialRequests { get; set; }
}

public class UserNotificationsPageViewModel
{
    public UserNotificationPreferencesForm Form { get; set; } = new();
    public List<UserNotificationItemViewModel> RecentNotifications { get; set; } = new();
}

public class UserNotificationPreferencesForm
{
    public bool ReservationEmail { get; set; }
    public bool ReservationPush { get; set; }
    public bool CheckInReminder { get; set; }
    public bool CancellationChanges { get; set; }
    public bool CampaignEmail { get; set; }
    public bool CampaignSms { get; set; }
    public bool SystemNotifications { get; set; }
}

public class UserNotificationItemViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
    public string TypeText { get; set; } = string.Empty;
}

public class UserSecurityPageViewModel
{
    public bool TwoFactorEnabled { get; set; }
    public List<UserSessionRowViewModel> Sessions { get; set; } = new();
}

public class UserSessionRowViewModel
{
    public string DeviceLabel { get; set; } = string.Empty;
    public string ActivityText { get; set; } = string.Empty;
    public string RememberText { get; set; } = string.Empty;
}

public class UserChangePasswordForm
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class UserTwoFactorForm
{
    public bool Enabled { get; set; }
}

public class UserPaymentMethodsPageViewModel
{
    public List<UserPaymentMethodRowViewModel> Methods { get; set; } = new();
    public UserPaymentMethodForm Form { get; set; } = new();
    public UserBillingSummaryViewModel Billing { get; set; } = new();
}

public class UserPaymentMethodRowViewModel
{
    public long PaymentMethodId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string DetailText { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

public class UserPaymentMethodForm
{
    public string CardLabel { get; set; } = string.Empty;
    public string CardHolder { get; set; } = string.Empty;
    public string Brand { get; set; } = "Visa";
    public string LastFourDigits { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public bool SetAsDefault { get; set; }
}

public class UserBillingSummaryViewModel
{
    public string InvoiceName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
