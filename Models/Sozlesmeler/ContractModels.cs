using otelturizmnew.Models.Paneller.Admin;

namespace otelturizmnew.Models.Sozlesmeler;

public class PublicContractPageViewModel
{
    public long ContractId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string SummaryHtml { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? LinkText { get; set; }
    public string? LinkUrl { get; set; }
    public string? RelatedLinksJson { get; set; }
    public string VersionText { get; set; } = string.Empty;
    public string EffectiveDateText { get; set; } = string.Empty;
    public string? ExpiryDateText { get; set; }
    public bool RequiresAcceptance { get; set; }
}

public class ContractAcceptanceAudienceModel
{
    public long UserId { get; set; }
    public long? PartnerId { get; set; }
    public long? FirmaId { get; set; }
    public string Audience { get; set; } = "user";
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public class ContractDefinitionModel
{
    public long Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string SummaryHtml { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? LinkText { get; set; }
    public string? LinkUrl { get; set; }
    public string? RelatedLinksJson { get; set; }
    public string VersionNo { get; set; } = "v1.0";
    public DateTime EffectiveAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool SendAfterEmailVerification { get; set; }
    public bool RequiresAcceptance { get; set; }
    public bool IsActive { get; set; }
    public string? EmailSubject { get; set; }
}

public class AdminContractManagementPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminSummaryCardViewModel> SummaryCards { get; set; } = new();
    public List<AdminContractRowViewModel> Contracts { get; set; } = new();
    public AdminContractEditForm Form { get; set; } = new();
}

public class AdminContractRowViewModel
{
    public long ContractId { get; set; }
    public string Audience { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string VersionText { get; set; } = string.Empty;
    public string DateRangeText { get; set; } = string.Empty;
    public string DeliveryText { get; set; } = string.Empty;
    public string AcceptanceText { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class AdminContractEditForm
{
    public long? ContractId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Audience { get; set; } = "user";
    public string Category { get; set; } = "kullanim-kosullari";
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string SummaryHtml { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? LinkText { get; set; }
    public string? LinkUrl { get; set; }
    public string? RelatedLinksJson { get; set; }
    public string VersionNo { get; set; } = "v1.0";
    public DateTime EffectiveAt { get; set; } = DateTime.Today;
    public DateTime? ExpiresAt { get; set; }
    public bool SendAfterEmailVerification { get; set; } = true;
    public bool RequiresAcceptance { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? EmailSubject { get; set; }
}
