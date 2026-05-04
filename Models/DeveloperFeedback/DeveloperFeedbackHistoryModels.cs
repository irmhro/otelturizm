namespace otelturizmnew.Models.DeveloperFeedback;

public sealed class DeveloperFeedbackHistoryItemViewModel
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FeedbackType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusTone { get; set; } = "neutral";
    public string Content { get; set; } = string.Empty;
    public string PageLabel { get; set; } = string.Empty;
    public string CreatedAtText { get; set; } = string.Empty;
    public string? AdminNote { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

public sealed class DeveloperFeedbackHistoryResponse
{
    public bool Success { get; set; }
    public List<DeveloperFeedbackHistoryItemViewModel> Items { get; set; } = new();
}

public sealed class DeveloperFeedbackActionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
