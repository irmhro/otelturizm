namespace otelturizmnew.Models.Giris;

public class UserSessionModel
{
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AccountType { get; set; } = "user";
    public string UserRole { get; set; } = "user";
    public long? PartnerId { get; set; }
    public long? OwnershipPartnerId { get; set; }
    public List<long> ManagedHotelIds { get; set; } = new();
    public List<string> RoleCodes { get; set; } = new();
}
