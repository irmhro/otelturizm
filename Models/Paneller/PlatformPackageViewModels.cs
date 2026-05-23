namespace otelturizmnew.Models.Paneller;

public sealed class PartnerPlatformPackagesPageViewModel
{
    public Partner.PartnerShellViewModel Shell { get; set; } = new();
    public long SelectedHotelId { get; set; }
    public string SelectedHotelName { get; set; } = string.Empty;
    public bool HotelHas5661Installed { get; set; }
    public bool HotelHas5651Installed { get; set; }
    public string? CategoryFilter { get; set; }
    public List<PlatformPackageCardViewModel> Packages { get; set; } = new();
    public List<PartnerPlatformPackageApplicationRowViewModel> Applications { get; set; } = new();
    public bool TablesReady { get; set; } = true;
}

public sealed class PartnerPlatformPackageDetailPageViewModel
{
    public Partner.PartnerShellViewModel Shell { get; set; } = new();
    public long SelectedHotelId { get; set; }
    public string SelectedHotelName { get; set; } = string.Empty;
    public PlatformPackageCardViewModel Package { get; set; } = new();
    public PartnerPlatformPackageApplicationFormModel Form { get; set; } = new();
    public bool Eligible { get; set; } = true;
    public string EligibilityMessage { get; set; } = string.Empty;
}

public sealed class PlatformPackageCardViewModel
{
    public long Id { get; set; }
    public string PackageCode { get; set; } = string.Empty;
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string DetailText { get; set; } = string.Empty;
    public decimal PriceAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string BillingPeriod { get; set; } = "Aylik";
    public string TargetRule { get; set; } = "HER_OTEL";
    public string CoverImageUrl { get; set; } = string.Empty;
    public List<string> GalleryUrls { get; set; } = new();
    public List<string> Features { get; set; } = new();
    public string? ContractUrl { get; set; }
    public string PriceText { get; set; } = string.Empty;
    public string TargetRuleBadge { get; set; } = string.Empty;
}

public sealed class PartnerPlatformPackageApplicationFormModel
{
    public long HotelId { get; set; }
    public long PackageId { get; set; }
    public bool Hotel5661InstalledDeclaration { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? PartnerNote { get; set; }
}

public sealed class PartnerPlatformPackageApplicationRowViewModel
{
    public long Id { get; set; }
    public string PackageTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PriceText { get; set; } = string.Empty;
    public string CreatedText { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}

public sealed class AdminPlatformPackagesPageViewModel
{
    public Admin.AdminShellViewModel Shell { get; set; } = new();
    public bool TablesReady { get; set; } = true;
    public List<PlatformPackageCardViewModel> Packages { get; set; } = new();
    public List<AdminPlatformPackageApplicationRowViewModel> Applications { get; set; } = new();
    public AdminPlatformPackageSummaryViewModel Summary { get; set; } = new();
}

public sealed class AdminPlatformPackageSummaryViewModel
{
    public int PendingCount { get; set; }
    public int ActiveCount { get; set; }
    public int PublishedPackageCount { get; set; }
}

public sealed class AdminPlatformPackageApplicationRowViewModel
{
    public long Id { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string PartnerEmail { get; set; } = string.Empty;
    public string PackageTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PriceText { get; set; } = string.Empty;
    public string CreatedText { get; set; } = string.Empty;
    public string PartnerNote { get; set; } = string.Empty;
    public string AdminNote { get; set; } = string.Empty;
    public bool Hotel5661Declared { get; set; }
}

public sealed class AdminPlatformPackageApplicationDecisionRequest
{
    public long ApplicationId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? AdminNote { get; set; }
}
