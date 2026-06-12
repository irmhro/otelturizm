namespace otelturizmnew.Services.Abstractions;

/// <summary>
/// 404 için kalıcı yönlendirme adayı (paket 221).
/// </summary>
public interface IDeadLinkRedirectService
{
    /// <summary>Eşleşme varsa hedef path (ör. /hotel), yoksa null.</summary>
    string? TryResolvePermanentRedirect(string originalPath);
}
