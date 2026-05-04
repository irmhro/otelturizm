namespace otelturizmnew.Models.Paneller.Partner;

public class PartnerFacilityUsersPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string? Warning { get; set; }
    public PartnerFacilityUserInviteRequest InviteForm { get; set; } = new();
    public List<PartnerFacilityUserRowViewModel> Users { get; set; } = new();
}

public class PartnerFacilityUserRowViewModel
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string Status { get; set; } = string.Empty; // Beklemede/Aktif/Iptal/SureDoldu
    public string? StartDateText { get; set; }
    public string? EndDateText { get; set; }
    public string? ApprovedAtText { get; set; }
    public string? InviteSentAtText { get; set; }
}

public class PartnerFacilityUserInviteRequest
{
    public long HotelId { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}

public class PartnerFacilityUserRevokeRequest
{
    public long HotelId { get; set; }
    public long AssignmentId { get; set; }
    public string? Reason { get; set; }
}

