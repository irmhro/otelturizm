using System.ComponentModel.DataAnnotations;

namespace otelturizmnew.Models.Paneller.Admin;

public sealed class AdminReviewModerationPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public string? Q { get; set; }
    public string? City { get; set; }
    public string? Hotel { get; set; }
    public int Take { get; set; } = 20;

    public List<AdminReviewModerationRowViewModel> Reviews { get; set; } = new();
    public List<AdminBlockedWordRowViewModel> BlockedWords { get; set; } = new();
    public List<AdminReviewTakedownRequestRowViewModel> TakedownRequests { get; set; } = new();
}

public sealed class AdminReviewModerationRowViewModel
{
    public long ReviewId { get; set; }
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public long UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public byte Score { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public short ReportCount { get; set; }
    public string CreatedText { get; set; } = string.Empty;
    public string CommentSnippet { get; set; } = string.Empty;
}

public sealed class AdminReviewModerationActionForm
{
    [Required]
    public long ReviewId { get; set; }

    public long HotelId { get; set; }

    [Required]
    public string Action { get; set; } = string.Empty; // approve|unpublish|reject

    public string? Note { get; set; }

    public string? ReturnUrl { get; set; }
}

public sealed class AdminReviewDeleteForm
{
    [Required]
    public long ReviewId { get; set; }

    public long HotelId { get; set; }

    public string? Note { get; set; }

    public string? ReturnUrl { get; set; }
}

public sealed class AdminReviewViolationNotifyForm
{
    [Required]
    public long ReviewId { get; set; }

    [Required]
    public long UserId { get; set; }

    public string? RuleSummary { get; set; }

    public string? AdminNote { get; set; }

    public string? ReturnUrl { get; set; }
}

public sealed class AdminBlockedWordAddForm
{
    [Required]
    [MaxLength(120)]
    public string Word { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Description { get; set; }

    public string? ReturnUrl { get; set; }
}

public sealed class AdminBlockedWordToggleForm
{
    [Required]
    public long Id { get; set; }

    public bool Active { get; set; }

    public string? ReturnUrl { get; set; }
}

public sealed class AdminBlockedWordRowViewModel
{
    public long Id { get; set; }
    public string Word { get; set; } = string.Empty;
    public bool Active { get; set; }
    public string? Description { get; set; }
    public string CreatedText { get; set; } = string.Empty;
}

public sealed class AdminReviewTakedownRequestRowViewModel
{
    public long RequestId { get; set; }
    public long ReviewId { get; set; }
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public long PartnerUserId { get; set; }
    public string PartnerEmail { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string CreatedText { get; set; } = string.Empty;
}

