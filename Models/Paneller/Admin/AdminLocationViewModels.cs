namespace otelturizmnew.Models.Paneller.Admin;

public enum AdminLocationEntityType
{
    City,
    District,
    Neighborhood
}

public class AdminLocationOptionViewModel
{
    public long Id { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class AdminLocationListPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public AdminLocationEntityType EntityType { get; set; }
    public string SearchTerm { get; set; } = string.Empty;
    public string ActiveFilter { get; set; } = string.Empty;
    public long? CountryIdFilter { get; set; }
    public long? CityIdFilter { get; set; }
    public long? DistrictIdFilter { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public List<AdminSummaryCardViewModel> SummaryCards { get; set; } = new();
    public List<AdminLocationOptionViewModel> CountryOptions { get; set; } = new();
    public List<AdminLocationOptionViewModel> CityOptions { get; set; } = new();
    public List<AdminLocationOptionViewModel> DistrictOptions { get; set; } = new();
    public List<AdminLocationRowViewModel> Rows { get; set; } = new();
}

public class AdminLocationRowViewModel
{
    public long Id { get; set; }
    public string PrimaryLabel { get; set; } = string.Empty;
    public string SecondaryLabel { get; set; } = string.Empty;
    public string TertiaryLabel { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string RegionLabel { get; set; } = string.Empty;
    public string UpdatedAtText { get; set; } = string.Empty;
}

public class AdminSkeletonPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public string PhaseNote { get; set; } = "Bu modül Faz 2 içerik geliştirmesine alınmıştır.";
    public List<string> PlannedFeatures { get; set; } = new();
}
