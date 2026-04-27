using Microsoft.Extensions.Configuration;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class FeatureFlagService : IFeatureFlagService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IGrowthGovernanceService _growthGovernance;

    public FeatureFlagService(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        IGrowthGovernanceService growthGovernance)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _growthGovernance = growthGovernance;
    }

    public bool IsEnabled(string flagKey)
    {
        if (_growthGovernance.AreAllGrowthFlagsDisabled)
        {
            return false;
        }

        var key = flagKey ?? string.Empty;
        var pct = _configuration.GetValue($"Growth:Flags:{key}:Percent", 0);
        if (pct <= 0)
        {
            return false;
        }

        if (pct >= 100)
        {
            return true;
        }

        var ctx = _httpContextAccessor.HttpContext;
        var fp = ctx?.Request.Cookies.TryGetValue("Otelturizm.ClientFp", out var v) == true ? v : "anon";
        var hash = HashCode.Combine(key, fp);
        var bucket = Math.Abs(hash % 100);
        return bucket < pct;
    }
}
