namespace otelturizmnew.Services.Abstractions;

public interface IPublicGrowthSignalsService
{
    /// <summary>Kısa TTL ile tutulan tahmini aktif görüntüleyici bandı (FOMO vitrin).</summary>
    int GetActiveViewerBand(long hotelId);
}
