namespace otelturizmnew.Models.Register;

public class PartnerRegistrationModel
{
    public string HotelName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyType { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactTitle { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string? Neighborhood { get; set; }
    public int? RoomCount { get; set; }
    public string TaxOffice { get; set; } = string.Empty;
    public string TaxNumber { get; set; } = string.Empty;
    public string ContactTcNo { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string? BankBranch { get; set; }
    public string Iban { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public bool AcceptAgreement { get; set; }
    public bool AcceptKvkk { get; set; }
    public bool DeclareAccurate { get; set; }
}
