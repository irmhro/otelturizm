using otelturizmnew.Models.Paneller.Common;
using otelturizmnew.Models.Paneller.User;

namespace otelturizmnew.Models.Paneller.Partner;

public class PartnerNotificationPreferencesPageViewModel
{
    public PartnerShellViewModel Shell { get; set; } = new();
    public UserNotificationPreferencesForm Form { get; set; } = new();
    public List<PartnerLoginHistoryRowViewModel> RecentLogins { get; set; } = new();
}

public class PartnerLoginHistoryRowViewModel
{
    public string TimeText { get; set; } = "—";
    public string IpAddress { get; set; } = "—";
    public string DurationText { get; set; } = "—";
    public string DeviceLabel { get; set; } = "—";
}

