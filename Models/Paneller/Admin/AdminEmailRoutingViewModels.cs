namespace otelturizmnew.Models.Paneller.Admin;

public class AdminEmailRoutingPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminEmailRoutingRowEditModel> Rows { get; set; } = new();
}

public class AdminEmailRoutingRowEditModel
{
    public string EventCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EmailsCsv { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    /// <summary>İlgili admin ekranına önerilen kısayol (salt okunur).</summary>
    public string DeepLinkHint { get; set; } = string.Empty;
}

public class AdminEmailRoutingSaveForm
{
    public List<AdminEmailRoutingRowInput> Rows { get; set; } = new();
}

public class AdminEmailRoutingRowInput
{
    public string EventCode { get; set; } = string.Empty;
    public string EmailsCsv { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
}
