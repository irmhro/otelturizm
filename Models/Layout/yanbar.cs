namespace otelturizmnew.Models.Layout;

public sealed class YanbarItem
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string IconClass { get; init; } = "fas fa-gift";
    public string Url { get; init; } = "/kampanyalar";
}

public static class Yanbar
{
    public static IReadOnlyList<YanbarItem> DefaultItems { get; } = new List<YanbarItem>
    {
        new()
        {
            Title = "Kampanyalar",
            Description = "Aktif kampanyaları keşfet",
            IconClass = "fas fa-badge-percent",
            Url = "/kampanyalar"
        },
        new()
        {
            Title = "İndirimler",
            Description = "İndirim fırsatlarını gör",
            IconClass = "fas fa-tags",
            Url = "/kampanyalar"
        }
    };
}
