namespace otelturizmnew.Models.Paneller.Admin;

public class AdminSystemHealthPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminSystemHealthCheckItemViewModel> Checks { get; set; } = new();
    public List<AdminSystemHealthQueueRowViewModel> Queues { get; set; } = new();
    public AdminInternalLinkCheckViewModel LinkCheck { get; set; } = new();

    /// <summary>ASP.NET Health Checks özet raporu (paket 246).</summary>
    public string PlatformHealthAggregateStatus { get; set; } = string.Empty;
    public double PlatformHealthTotalDurationMs { get; set; }
    public List<AdminPlatformHealthProbeViewModel> PlatformHealthProbes { get; set; } = new();
    public string? PlatformHealthError { get; set; }
}

public sealed class AdminPlatformHealthProbeViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double DurationMs { get; set; }
    public string? Detail { get; set; }
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

public class AdminInternalLinkCheckViewModel
{
    public string SourceFileRelativePath { get; set; } = "tools/Health/routes-extracted-from-views.txt";
    public string BaseUrl { get; set; } = string.Empty;
    public DateTimeOffset? CheckedAtUtc { get; set; }
    public int Total { get; set; }
    public int Ok { get; set; }
    public int Bad { get; set; }
    public List<AdminInternalLinkCheckRowViewModel> Rows { get; set; } = new();
    public string? Warning { get; set; }
}

public class AdminInternalLinkCheckRowViewModel
{
    public string Route { get; set; } = string.Empty;
    public int Status { get; set; }
    public int Ms { get; set; }
    public bool IsOk => Status is >= 200 and < 400;
    public string StatusText => Status < 0 ? "Hata" : Status.ToString();
}

