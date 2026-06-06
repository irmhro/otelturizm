using System.ComponentModel.DataAnnotations;
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
    public string FavoriteSortFilter { get; set; } = "newest";
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
    public string SearchTerm { get; set; } = string.Empty;
    public string SortFilter { get; set; } = "newest";
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
    public long RoomTypeId { get; set; }
    public string GuestText { get; set; } = string.Empty;
    public string MealOrRoomText { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string StatusTone { get; set; } = "ok";
    public string SubNote { get; set; } = string.Empty;
    public string SubNoteTone { get; set; } = "info";
    public decimal TotalAmount { get; set; }
    public string TotalText { get; set; } = string.Empty;
    public bool CanCancel { get; set; }
    public bool CanUpdateDates { get; set; }
    public bool IsUpcoming { get; set; }
    public bool IsCancelled { get; set; }
    public string? CancellationReason { get; set; }
    public string? CancellationTimeText { get; set; }
    public long HotelId { get; set; }
    public string OtelOnayDurumu { get; set; } = string.Empty;
    public bool CanSubmitReview { get; set; }

    // Detay ekranı / notlar
    public string GuestName { get; set; } = string.Empty;
    public string GuestEmail { get; set; } = string.Empty;
    public string GuestPhone { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string CreatedAtText { get; set; } = string.Empty;
    public string GuestNote { get; set; } = string.Empty;
    public string RequestNote { get; set; } = string.Empty;
}

public class UserReservationNoteForm
{
    public long ReservationId { get; set; }
    public string Note { get; set; } = string.Empty;
}

public class UserReservationDateUpdateForm
{
    public long ReservationId { get; set; }
    public string CheckInDate { get; set; } = string.Empty;
    public string CheckOutDate { get; set; } = string.Empty;
}

public class UserReservationReviewPageViewModel
{
    public long ReservationId { get; set; }
    public string ReservationNo { get; set; } = string.Empty;
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string StayDateText { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public UserReservationReviewForm Form { get; set; } = new();
}

public class UserReservationReviewForm
{
    public long ReservationId { get; set; }
    public string TravelProfile { get; set; } = string.Empty;
    [Range(1, 5)]
    public int SatisfactionLevel { get; set; } = 3;
    [Range(1, 10)]
    public int PuanKonum { get; set; } = 8;
    [Range(1, 10)]
    public int PuanTemizlik { get; set; } = 8;
    [Range(1, 10)]
    public int PuanFiyat { get; set; } = 8;
    [Range(1, 10)]
    public int PuanPersonel { get; set; } = 8;
    [Range(1, 10)]
    public int PuanSessizlik { get; set; } = 8;
    [Range(1, 10)]
    public int PuanUlasim { get; set; } = 8;
    public string Comment { get; set; } = string.Empty;
    public bool Anonymous { get; set; }
}

public class UserReviewsPageViewModel
{
    public int TotalCount { get; set; }
    public int WaitingReviewCount { get; set; }
    public int ReviewedCount { get; set; }
    public string StatusFilter { get; set; } = "all";
    public string SearchTerm { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 5;
    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public List<UserReviewReservationRowViewModel> Items { get; set; } = new();
}

public class UserReviewReservationRowViewModel
{
    public long ReservationId { get; set; }
    public string ReservationNo { get; set; } = string.Empty;
    public string ReservationNoTail => string.IsNullOrWhiteSpace(ReservationNo)
        ? "-"
        : ReservationNo.Length <= 3 ? ReservationNo : ReservationNo[^3..];
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string HotelSlug { get; set; } = string.Empty;
    public string? HotelImageUrl { get; set; }
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string StayDateText { get; set; } = string.Empty;
    public string ReservationStatusText { get; set; } = string.Empty;
    public string ReviewStatusText { get; set; } = "Yorum bekliyor";
    public string ReviewTone { get; set; } = "waiting";
    public bool HasReview { get; set; }
    public bool CanWriteReview { get; set; }
    public bool CanEditReview { get; set; }
    public long? ReviewId { get; set; }
    public string ReviewText { get; set; } = string.Empty;
    public string ReviewDateText { get; set; } = string.Empty;
    public string EditLimitText { get; set; } = string.Empty;
    public decimal ReviewScore { get; set; }

    /// <summary>Listede gösterilecek kısaltılmış yorum (tam metin düzenleme alanında kalır).</summary>
    public string ReviewSnippet
    {
        get
        {
            var t = (ReviewText ?? string.Empty).Trim();
            if (t.Length == 0)
            {
                return string.Empty;
            }

            const int max = 160;
            if (t.Length <= max)
            {
                return t;
            }

            return t[..max].TrimEnd() + "…";
        }
    }

    public bool ReviewSnippetTruncated => HasReview && (ReviewText ?? string.Empty).Trim().Length > 160;
}

public class UserReviewUpdateForm
{
    public long ReviewId { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public class UserReviewDeleteForm
{
    public long ReviewId { get; set; }
}

public class UserReservationCancelForm
{
    public long ReservationId { get; set; }
    public string CancellationReason { get; set; } = string.Empty;
}

public class UserFavoriteSummaryViewModel
{
    public string HotelName { get; set; } = string.Empty;
    public string HotelSlug { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string RatingText { get; set; } = string.Empty;
    public string ReservationCountText { get; set; } = string.Empty;
    public string AddedDateText { get; set; } = string.Empty;
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
    public List<UserLoyaltyRecentCityViewModel> RecentReservationCities { get; set; } = new();
    public List<UserLoyaltyTravelPlanViewModel> TravelPlans { get; set; } = new();
    public List<UserLoyaltyOfferViewModel> Offers { get; set; } = new();
    public List<UserLoyaltyBudgetPlanViewModel> BudgetPlans { get; set; } = new();
    public List<UserLoyaltyRecommendationViewModel> Recommendations { get; set; } = new();
    public UserLoyaltyBudgetPlanForm BudgetPlanForm { get; set; } = new();
    public UserLoyaltyTravelPlanForm TravelPlanForm { get; set; } = new();
}

public class UserLoyaltyRecentCityViewModel
{
    public string CityName { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
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
    public bool EmailVerified { get; set; }
    public string EmailVerifiedAtText { get; set; } = "—";
    public string ProfileImageUrl { get; set; } = "/uploads/demo/avatars/avatar-01.svg";
    public List<string> PresetAvatarUrls { get; set; } = new();
    public List<UserUploadedProfileAvatarViewModel> UploadedProfileAvatars { get; set; } = new();
    public UserPhoneVerificationStatusViewModel PhoneVerification { get; set; } = new();
    public List<AddressCountryOption> Countries { get; set; } = new();
    public List<AddressProvinceOption> Provinces { get; set; } = new();
    public long? SelectedCountryId { get; set; }
    public long? SelectedProvinceId { get; set; }
    public long? SelectedDistrictId { get; set; }
    public long? SelectedNeighborhoodId { get; set; }
    public bool OpenCompletionModal { get; set; }
    public bool OpenPhoneVerification { get; set; }
    public bool OpenEmailUpdate { get; set; }
    public string ReturnUrl { get; set; } = string.Empty;
    public List<string> RoomPreferenceOptions { get; set; } = new();
    public List<string> BedPreferenceOptions { get; set; } = new();
    public List<string> SpokenLanguageOptions { get; set; } = new();
    public List<string> TravelPurposeOptions { get; set; } = new();
}

public class UserUploadedProfileAvatarViewModel
{
    public long FileId { get; set; }
    public string AccessUrl { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string UploadedAtText { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
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
    /// <summary>Form gönderiminde seçilen ülke (ULKELER.ID).</summary>
    public long? UlkeId { get; set; }
    /// <summary>Form gönderiminde seçilen il (ILLER.ID).</summary>
    public long? IlId { get; set; }
    /// <summary>Form gönderiminde seçilen ilçe (ILCELER.ID).</summary>
    public long? IlceId { get; set; }
    /// <summary>Form gönderiminde seçilen mahalle (MAHALLELER.ID).</summary>
    public long? MahalleId { get; set; }
    public string? PostalCode { get; set; }
    public string? RoomPreference { get; set; }
    public string? BedPreference { get; set; }
    public string? SpokenLanguages { get; set; }
    public string? TravelPurpose { get; set; }
    public string? SpecialRequests { get; set; }
}

public class UserTravelPreferencesForm
{
    public string? ReturnUrl { get; set; }
    public string? RoomPreference { get; set; }
    public string? BedPreference { get; set; }
    public string? SpokenLanguages { get; set; }
    public string? TravelPurpose { get; set; }
    public string? SpecialRequests { get; set; }
}

public class UserEmailUpdateRequestForm
{
    public string? ReturnUrl { get; set; }
    public string NewEmail { get; set; } = string.Empty;
}

public class UserEmailUpdateVerifyForm
{
    public string? ReturnUrl { get; set; }
    public string NewEmail { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Token { get; set; }
}

public class UserNotificationsPageViewModel
{
    public UserNotificationPreferencesForm Form { get; set; } = new();
    public List<UserNotificationItemViewModel> RecentNotifications { get; set; } = new();
}

public class UserNotificationPreferencesForm
{
    public bool ReservationEmail { get; set; } = true;
    public bool ReservationPush { get; set; } = true;
    public bool CheckInReminder { get; set; } = true;
    public bool CancellationChanges { get; set; } = true;
    public bool CampaignEmail { get; set; }
    public bool CampaignSms { get; set; }
    public bool SystemNotifications { get; set; } = true;
    public bool LoginEmail { get; set; }
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
    public string SelectedTwoFactorChannel { get; set; } = "email";
    public string EmailAddress { get; set; } = string.Empty;
    public string MaskedEmailAddress { get; set; } = string.Empty;
    public bool EmailUsableForTwoFactor { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string MaskedPhoneNumber { get; set; } = string.Empty;
    public bool PhoneUsableForTwoFactor { get; set; }
    public UserPhoneVerificationStatusViewModel PhoneVerification { get; set; } = new();
    public List<UserSessionRowViewModel> Sessions { get; set; } = new();
}

public class UserSessionRowViewModel
{
    public string DeviceLabel { get; set; } = string.Empty;
    public string IpAddress { get; set; } = "—";
    public string LoginAtText { get; set; } = "—";
    public int OpenMinutes { get; set; }
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
    public string Channel { get; set; } = "email";
}

public class UserPaymentMethodsPageViewModel
{
    public List<UserPaymentMethodRowViewModel> Methods { get; set; } = new();
    public UserPaymentMethodForm Form { get; set; } = new();
    public UserBillingSummaryViewModel Billing { get; set; } = new();
    public UserBillingForm BillingForm { get; set; } = new();
}

public class UserBillingForm
{
    public string InvoiceName { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UserLoyaltyRedeemForm
{
    public long RewardId { get; set; }
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

public class UserInvoicesPageViewModel
{
    public bool TableMissing { get; set; }
    public List<UserInvoiceRowViewModel> Rows { get; set; } = new();
}

public class UserInvoiceRowViewModel
{
    public long ReservationId { get; set; }
    public string ReservationNo { get; set; } = string.Empty;
    public string HotelName { get; set; } = string.Empty;
    public string StayText { get; set; } = string.Empty;
    public string TotalText { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public bool HasInvoice { get; set; }
    public string? DownloadUrl { get; set; }
    public string? UploadedAtText { get; set; }
    public string? MimeType { get; set; }
    public DateTime? CheckOutDate { get; set; }

    public bool IsPdf =>
        HasInvoice && (
            string.Equals(MimeType, "application/pdf", StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(DownloadUrl) && DownloadUrl.Contains(".pdf", StringComparison.OrdinalIgnoreCase)));
}
