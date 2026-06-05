namespace otelturizmnew.Models.Legal;

public class ContractLinkViewModel
{
    public long ContractId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Url => $"/sozlesmeler/{Slug}";
    public string ContractType { get; set; } = string.Empty;
}

public class ContractDetailPageViewModel
{
    public long ContractId { get; set; }
    public string Audience { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? SummaryHtml { get; set; }
    public string ContentHtml { get; set; } = string.Empty;
    public string? HeroImageUrl { get; set; }
    public string? PdfUrl { get; set; }
    public string VersionText { get; set; } = string.Empty;
    public string EffectiveDateText { get; set; } = string.Empty;
    public List<ContractLinkViewModel> RelatedContracts { get; set; } = new();
}

public class ContractAcceptanceRegistrationRequest
{
    public long UserId { get; set; }
    public long? PartnerId { get; set; }
    public long? FirmaId { get; set; }
    public string Audience { get; set; } = "user";
    public string Email { get; set; } = string.Empty;
    public bool IncludePrimaryAgreement { get; set; }
    public bool IncludeKvkk { get; set; }
    public string Source { get; set; } = "register";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
