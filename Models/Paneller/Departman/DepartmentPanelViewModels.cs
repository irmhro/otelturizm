namespace otelturizmnew.Models.Paneller.Departman;

public sealed class DepartmentPanelShellViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string ActiveDepartment { get; set; } = "kullanici";
    public string PanelTitle { get; set; } = "Departman Paneli";
    public string PanelSubtitle { get; set; } = string.Empty;
    public List<DepartmentPanelNavItemViewModel> Departments { get; set; } = new();
}

public sealed class DepartmentPanelNavItemViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-layer-group";
    public bool IsActive { get; set; }
}

public sealed class DepartmentDashboardPageViewModel
{
    public DepartmentPanelShellViewModel Shell { get; set; } = new();
    public List<DepartmentMetricCardViewModel> Metrics { get; set; } = new();
    public List<DepartmentWorkItemViewModel> WorkItems { get; set; } = new();
    public List<DepartmentProtocolItemViewModel> Protocols { get; set; } = new();
}

public sealed class DepartmentMetricCardViewModel
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = "0";
    public string Description { get; set; } = string.Empty;
    public string ToneClass { get; set; } = "info";
    public string IconClass { get; set; } = "fa-chart-simple";
}

public sealed class DepartmentWorkItemViewModel
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string ToneClass { get; set; } = "info";
    public string ActionUrl { get; set; } = "#";
}

public sealed class DepartmentProtocolItemViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string StatusText { get; set; } = "Planlandı";
}
