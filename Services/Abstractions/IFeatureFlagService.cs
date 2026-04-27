namespace otelturizmnew.Services.Abstractions;

public interface IFeatureFlagService
{
    /// <summary>Yüzde bazlı rollout; anonim kullanıcı için fingerprint çerezi ile deterministik örnekleme.</summary>
    bool IsEnabled(string flagKey);
}
