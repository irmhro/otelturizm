using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using otelturizmnew.Models.Paneller.Admin;

namespace otelturizmnew.Models.Paneller.Developer;

public class DeveloperShellViewModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PanelTitle { get; set; } = string.Empty;
    public string PanelSubtitle { get; set; } = string.Empty;
    public int OpenRequestCount { get; set; }
    public int AssignedRequestCount { get; set; }
    public int ReviewRequestCount { get; set; }
    public int CompletedRequestCount { get; set; }
}

public class DeveloperDashboardViewModel
{
    public DeveloperShellViewModel Shell { get; set; } = new();
    public List<DeveloperStatCardViewModel> Stats { get; set; } = new();
    public DeveloperRequestCreateForm CreateForm { get; set; } = new();
    public List<DeveloperRequestCardViewModel> Requests { get; set; } = new();
    public string SearchText { get; set; } = string.Empty;
    public string StatusFilter { get; set; } = "all";
}

public class DeveloperStatCardViewModel
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ToneClass { get; set; } = "info";
    public string IconClass { get; set; } = "fa-code";
}

public class DeveloperRequestCardViewModel
{
    public long RequestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Orta";
    public string PriorityToneClass { get; set; } = "warning";
    public string Status { get; set; } = "Yeni";
    public string StatusToneClass { get; set; } = "info";
    public string CreatedAtText { get; set; } = string.Empty;
    public string LastActivityText { get; set; } = string.Empty;
    public string CreatorName { get; set; } = string.Empty;
    public long CreatorUserId { get; set; }
    public string AssignedDeveloperName { get; set; } = string.Empty;
    public long? AssignedDeveloperUserId { get; set; }
    public string? PlannedStartDateText { get; set; }
    public string? DueDateText { get; set; }
    public string? CompletedAtText { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsAssignedToCurrentDeveloper { get; set; }
    public bool IsCreatedByCurrentDeveloper { get; set; }
    public bool CanDeveloperReply { get; set; }
    public bool CanAdminManage { get; set; }
    public List<DeveloperRequestActivityViewModel> Activities { get; set; } = new();
    public DeveloperRequestReplyForm ReplyForm { get; set; } = new();
    public AdminDevelopmentRequestUpdateForm AdminForm { get; set; } = new();
}

public class DeveloperRequestActivityViewModel
{
    public long ActivityId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string ActivityLabel { get; set; } = string.Empty;
    public string SourceRole { get; set; } = string.Empty;
    public string SourceRoleLabel { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string CreatedAtText { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class DeveloperUserOptionViewModel
{
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}

public class DeveloperRequestCreateForm
{
    [Required]
    [StringLength(220, MinimumLength = 6)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(6000, MinimumLength = 20)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Priority { get; set; } = "Orta";

    public IFormFile? VisualFile { get; set; }
}

public class DeveloperRequestReplyForm
{
    public long RequestId { get; set; }

    [Required]
    public string ActionType { get; set; } = "comment";

    [Required]
    [StringLength(4000, MinimumLength = 5)]
    public string Message { get; set; } = string.Empty;

    public IFormFile? VisualFile { get; set; }
}

public class AdminDevelopmentRequestUpdateForm
{
    public long RequestId { get; set; }

    [Required]
    [StringLength(220, MinimumLength = 6)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(6000, MinimumLength = 20)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string Priority { get; set; } = "Orta";

    [Required]
    public string Status { get; set; } = "Yeni";

    public long? AssignedDeveloperUserId { get; set; }
    public DateOnly? PlannedStartDate { get; set; }
    public DateOnly? DueDate { get; set; }

    [StringLength(4000)]
    public string ReplyMessage { get; set; } = string.Empty;

    public IFormFile? VisualFile { get; set; }
}

public class AdminDevelopmentRequestsPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminSummaryCardViewModel> SummaryCards { get; set; } = new();
    public List<DeveloperUserOptionViewModel> Developers { get; set; } = new();
    public List<DeveloperRequestCardViewModel> Requests { get; set; } = new();
    public string SearchText { get; set; } = string.Empty;
    public string StatusFilter { get; set; } = "all";
    public string PriorityFilter { get; set; } = "all";
    public long? DeveloperFilterUserId { get; set; }
}
