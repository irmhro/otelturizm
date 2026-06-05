namespace otelturizmnew.Models.Register;

public class FirmaRegistrationModel
{
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyType { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public int? EmployeeCount { get; set; }
    public decimal? MonthlyTravelBudget { get; set; }
    public string TaxNumber { get; set; } = string.Empty;
    public string TaxOffice { get; set; } = string.Empty;
    public string? TradeRegistryNumber { get; set; }
    public string? MersisNumber { get; set; }
    public string CompanyEmail { get; set; } = string.Empty;
    public string CompanyPhone { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string ContactTitle { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string? Neighborhood { get; set; }
    public string? CountryName { get; set; }
    public long? UlkeId { get; set; }
    public long? IlId { get; set; }
    public long? IlceId { get; set; }
    public long? MahalleId { get; set; }
    public string? PostalCode { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public bool AcceptAgreement { get; set; }
    public bool AcceptKvkk { get; set; }
}
