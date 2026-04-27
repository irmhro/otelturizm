namespace otelturizmnew.Services.Abstractions;

/// <summary>Feature flag kill-switch ve acil durum yönetimi (paket 230).</summary>
public interface IGrowthGovernanceService
{
    bool AreAllGrowthFlagsDisabled { get; }

    void SetEmergencyKillSwitch(bool active);

    bool EmergencyKillSwitchActive { get; }
}
