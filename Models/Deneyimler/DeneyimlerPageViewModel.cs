namespace otelturizmnew.Models.Deneyimler;

public class DeneyimlerPageViewModel
{
    public string HeroEyebrow { get; set; } = string.Empty;
    public string HeroTitle { get; set; } = string.Empty;
    public string HeroLead { get; set; } = string.Empty;
    public List<DeneyimStatViewModel> Stats { get; set; } = new();
    public List<DeneyimCategoryViewModel> Categories { get; set; } = new();
    public List<DeneyimCardViewModel> Featured { get; set; } = new();
    public List<DeneyimCardViewModel> Collections { get; set; } = new();
    public List<DeneyimStoryViewModel> Stories { get; set; } = new();
    public List<DeneyimJourneyStepViewModel> JourneySteps { get; set; } = new();
    public List<DeneyimSmartRouteViewModel> SmartRoutes { get; set; } = new();
    public DeneyimSpotlightViewModel? Spotlight { get; set; }
    public List<DeneyimMoodPickViewModel> MoodPicks { get; set; } = new();
    public List<DeneyimCampaignHighlightViewModel> CampaignHighlights { get; set; } = new();
    public List<string> LiveTicker { get; set; } = new();
}

public class DeneyimStatViewModel
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class DeneyimCategoryViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Icon { get; set; } = "fa-compass";
    public string Emoji { get; set; } = string.Empty;
}

public class DeneyimCardViewModel
{
    public string Key { get; set; } = string.Empty;
    public string CategoryKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Mood { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string Stamp { get; set; } = string.Empty;
    public string Accent { get; set; } = "#E30A17";
    public string Gradient { get; set; } = "linear-gradient(145deg, #1a1919 0%, #003b95 100%)";
    public string ImageUrl { get; set; } = string.Empty;
    public string LinkUrl { get; set; } = "/oteller";
    public bool IsFeatured { get; set; }
    public bool IsWide { get; set; }
}

public class DeneyimStoryViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Quote { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Accent { get; set; } = "#FF385C";
}

public class DeneyimJourneyStepViewModel
{
    public int Step { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Icon { get; set; } = "fa-location-dot";
}

public class DeneyimSmartRouteViewModel
{
    public string Hashtag { get; set; } = string.Empty;
    public string SearchQuery { get; set; } = string.Empty;
    public string ColorClass { get; set; } = "sage";
    public string LinkUrl { get; set; } = "/oteller";
}

public class DeneyimSpotlightViewModel
{
    public string Badge { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string LinkUrl { get; set; } = "/oteller";
    public string Accent { get; set; } = "#E30A17";
    public string Emoji { get; set; } = "✨";
    public string ImageUrl { get; set; } = string.Empty;
}

public class DeneyimMoodPickViewModel
{
    public string CategoryKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public string Hint { get; set; } = string.Empty;
}

public class DeneyimCampaignHighlightViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string LinkUrl { get; set; } = "/kampanyalar";
    public string ImageUrl { get; set; } = string.Empty;
}
