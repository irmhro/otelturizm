namespace otelturizmnew.Models.Paneller.Common;

public static class PanelHeaderAudience
{
    public const string User = "user";
    public const string Partner = "partner";
    public const string Firma = "firma";
    public const string Sales = "sales";
}

public sealed class HeaderBildiriViewModel
{
    public const int DropdownItemLimit = 7;

    public string PanelKey { get; set; } = PanelHeaderAudience.User;
    public string PanelLabel { get; set; } = "Kullanici";
    public List<HeaderBildiriItemViewModel> Items { get; set; } = new();
    public int AllItemsCount { get; set; }
    public int UnreadCount { get; set; }
    public int TotalCount => AllItemsCount > 0 ? AllItemsCount : Items.Count(static item => !item.IsPlaceholder);
    public bool HasMoreItems => AllItemsCount > Items.Count(static item => !item.IsPlaceholder);
    public string InboxUrl { get; set; } = "/panel/user/bildirimlerim";
}

public sealed class HeaderBildiriItemViewModel
{
    public string ItemKey { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-bell";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Tone { get; set; } = "info";
    public string TimeLabel { get; set; } = "Simdi";
    public string AbsoluteTimeLabel { get; set; } = "Zaman bilgisi yok";
    public string Url { get; set; } = "#";
    public bool IsRead { get; set; }
    public bool IsPlaceholder { get; set; }
    public DateTime? EventTimeUtc { get; set; }
}

public sealed class HeaderBildiriReadRequest
{
    public string PanelKey { get; set; } = PanelHeaderAudience.User;
    public List<string> ItemKeys { get; set; } = new();
}

public sealed class HeaderBildiriClearRequest
{
    public string PanelKey { get; set; } = PanelHeaderAudience.User;
}
