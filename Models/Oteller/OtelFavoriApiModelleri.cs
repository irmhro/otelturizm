namespace otelturizmnew.Models.Oteller;

public class OtelFavoriToggleIstek
{
    public long HotelId { get; set; }
    public string SourcePage { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
}

public class OtelFavoriToggleYanit
{
    public bool Success { get; set; }
    public bool IsFavorite { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? LoginUrl { get; set; }
    public string? RegisterUrl { get; set; }
}
