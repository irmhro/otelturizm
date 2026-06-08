namespace otelturizmnew.Models;

public class HotelCompletenessRuleResult
{
    public string FieldKey { get; set; } = string.Empty;
    public string FieldLabel { get; set; } = string.Empty;
    public string Severity { get; set; } = "warning";
    public string AdminTabTarget { get; set; } = "tab-genel";
    public string PartnerFixPath { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-circle-exclamation";
    public bool IsMissing { get; set; }
}

public class HotelCompletenessSnapshot
{
    public int Score { get; set; }
    public int TotalRules { get; set; }
    public int CompletedRules { get; set; }
    public int MissingCount { get; set; }
    public int CriticalMissingCount { get; set; }
    public List<HotelCompletenessRuleResult> Rules { get; set; } = new();
    public List<HotelCompletenessRuleResult> MissingRules => Rules.Where(x => x.IsMissing).ToList();
}

public class PartnerHotelCompletenessViewModel
{
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public int CompletenessScore { get; set; }
    public int MissingCount { get; set; }
    public int CriticalMissingCount { get; set; }
    public List<PartnerCompletenessItemViewModel> MissingItems { get; set; } = new();
}

public class PartnerCompletenessItemViewModel
{
    public string FieldKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Severity { get; set; } = "warning";
    public string FixUrl { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-circle-exclamation";
}
