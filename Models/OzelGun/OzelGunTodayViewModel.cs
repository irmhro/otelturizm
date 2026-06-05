namespace otelturizmnew.Models.OzelGun;

public sealed class OzelGunTodayViewModel
{
    public string GunKodu { get; init; } = string.Empty;
    public string GunAdi { get; init; } = string.Empty;
    public string KutlamaMetni { get; init; } = string.Empty;
    public string Emoji { get; init; } = string.Empty;
    public string Kategori { get; init; } = string.Empty;

    public string DisplayText =>
        string.IsNullOrWhiteSpace(KutlamaMetni)
            ? GunAdi
            : KutlamaMetni;
}
