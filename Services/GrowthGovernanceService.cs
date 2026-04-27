using Microsoft.Extensions.Configuration;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public sealed class GrowthGovernanceService : IGrowthGovernanceService
{
    private readonly IConfiguration _configuration;
    private volatile bool _emergencyKill;

    public GrowthGovernanceService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool EmergencyKillSwitchActive => _emergencyKill;

    public bool AreAllGrowthFlagsDisabled =>
        _configuration.GetValue("Growth:KillSwitchAll", false) || _emergencyKill;

    public void SetEmergencyKillSwitch(bool active)
    {
        _emergencyKill = active;
    }
}
