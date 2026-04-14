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
}

public class UserReservationsPageViewModel
{
    public int UpcomingCount { get; set; }
    public int PastCount { get; set; }
    public int CancelledCount { get; set; }
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
    public List<UserMessageThreadViewModel> Threads { get; set; } = new();
    public long? SelectedConversationId { get; set; }
    public string SelectedTitle { get; set; } = "Mesajlarım";
    public string SelectedSubtitle { get; set; } = "Mesaj detayları";
    public List<UserMessageItemViewModel> Messages { get; set; } = new();
}

public class UserMessageThreadViewModel
{
    public long ConversationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Preview { get; set; } = string.Empty;
    public string AvatarText { get; set; } = "OT";
    public string AvatarTone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class UserMessageItemViewModel
{
    public bool IsOutgoing { get; set; }
    public string MessageText { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
}

public class UserMessageSendForm
{
    public long ConversationId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class UserProfilePageViewModel
{
    public UserProfileForm Form { get; set; } = new();
}

public class UserProfileForm
{
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
