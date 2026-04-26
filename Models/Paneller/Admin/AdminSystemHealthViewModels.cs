namespace otelturizmnew.Models.Paneller.Admin;

public class AdminSystemHealthPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminSystemHealthCheckItemViewModel> Checks { get; set; } = new();
    public List<AdminSystemHealthQueueRowViewModel> Queues { get; set; } = new();
}

public class AdminSystemHealthCheckItemViewModel
{
    public string Title { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string ToneClass { get; set; } = "info"; // success/warning/danger/info
    public string Detail { get; set; } = string.Empty;
}

public class AdminSystemHealthQueueRowViewModel
{
    public string QueueName { get; set; } = string.Empty;
    public int PendingCount { get; set; }
    public int FailedCount { get; set; }
    public string OldestPendingText { get; set; } = "-";
    public string Note { get; set; } = string.Empty;
}

