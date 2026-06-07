namespace otelturizmnew.Services.Abstractions;

public interface IDawnSurpriseService
{
    bool IsEligible(HttpContext httpContext);
    DawnSurpriseState? GetActive(HttpContext httpContext);
    DawnSurpriseOpenResult? Open(HttpContext httpContext);
    void Clear(HttpContext httpContext);
}

public sealed class DawnSurpriseState
{
    public int Percent { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }

    public int RemainingSeconds =>
        Math.Max(0, (int)Math.Ceiling((ExpiresAt - DateTimeOffset.UtcNow).TotalSeconds));
}

public sealed class DawnSurpriseOpenResult
{
    public int Percent { get; init; }
    public bool IsNew { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public int RemainingSeconds =>
        Math.Max(0, (int)Math.Ceiling((ExpiresAt - DateTimeOffset.UtcNow).TotalSeconds));
}
