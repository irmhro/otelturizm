namespace otelturizmnew.Models.Paneller.Admin;

public class AdminSupportArticlePageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public string SearchText { get; set; } = string.Empty;
    public long? CategoryIdFilter { get; set; }
    public string StatusFilter { get; set; } = "all";
    public long? EditArticleId { get; set; }
    public List<AdminSupportArticleCategoryOptionViewModel> Categories { get; set; } = new();
    public List<AdminSummaryCardViewModel> SummaryCards { get; set; } = new();
    public List<AdminSupportArticleRowViewModel> Articles { get; set; } = new();
    public AdminSupportArticleForm Form { get; set; } = new();
}

public class AdminSupportArticleCategoryOptionViewModel
{
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}

public class AdminSupportArticleRowViewModel
{
    public long ArticleId { get; set; }
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-circle-question";
    public bool IsFeatured { get; set; }
    public bool ShowInHelpCenter { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public string UpdatedAtText { get; set; } = "-";
}

public class AdminSupportArticleForm
{
    public long? ArticleId { get; set; }
    public long CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SeoSlug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-circle-question";
    public bool IsFeatured { get; set; }
    public bool ShowInHelpCenter { get; set; } = true;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class AdminSupportArticleActionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long? ArticleId { get; set; }
}
