namespace otelturizmnew.Models.Paneller.Admin;

public class AdminContractManagementPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminSummaryCardViewModel> SummaryCards { get; set; } = new();
    public List<AdminContractRowViewModel> Contracts { get; set; } = new();
    public AdminContractForm Form { get; set; } = new();
}

public class AdminContractRowViewModel
{
    public long ContractId { get; set; }
    public string Audience { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string VersionText { get; set; } = string.Empty;
    public string EffectiveRangeText { get; set; } = string.Empty;
    public string AcceptanceText { get; set; } = string.Empty;
    public string DeliveryText { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class AdminContractForm
{
    public long? ContractId { get; set; }
    public string Audience { get; set; } = "user";
    public string ContractType { get; set; } = "agreement";
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? SummaryHtml { get; set; }
    public string ContentHtml { get; set; } = string.Empty;
    public string? HeroImageUrl { get; set; }
    public string? ContractUrl { get; set; }
    public int VersionNo { get; set; } = 1;
    public DateTime EffectiveStartDate { get; set; } = DateTime.Today;
    public DateTime? EffectiveEndDate { get; set; }
    public bool RequiresAcceptance { get; set; } = true;
    public bool SendOnEmailVerification { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public bool ForceResendAfterSave { get; set; }
    public string? Note { get; set; }
}
