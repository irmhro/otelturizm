namespace otelturizmnew.Models.Destek;

public class YardimMerkeziViewModel
{
    public string SearchTerm { get; set; } = string.Empty;
    public List<DestekKategoriViewModel> Categories { get; set; } = new();
    public List<DestekMakaleViewModel> PopularTopics { get; set; } = new();
    public List<DestekKanalViewModel> SupportChannels { get; set; } = new();
    public DestekKanalViewModel? LiveChatChannel { get; set; }
    public List<YardimMerkeziTeamMemberViewModel> PlatformTeam { get; set; } = new();
    public List<YardimMerkeziTeamMemberViewModel> DepartmentTeam { get; set; } = new();
    public List<YardimMerkeziTeamMemberViewModel> DeveloperTeam { get; set; } = new();
}

public class YardimMerkeziTeamMemberViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string TeamType { get; set; } = "department";
}

public class SssViewModel
{
    public string SearchTerm { get; set; } = string.Empty;
    public string ActiveCategorySlug { get; set; } = "tumu";
    public List<SssKategoriViewModel> Categories { get; set; } = new();
    public List<SssBolumViewModel> Sections { get; set; } = new();
    public DestekKanalViewModel? CtaChannel { get; set; }
}

public class DestekKategoriViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-circle-info";
    public string ColorHex { get; set; } = "#003B95";
    public string LinkUrl { get; set; } = "#";
    public string? HeroTitle { get; set; }
    public string? HeroSubtitle { get; set; }
    public string? HeroImageUrl { get; set; }
    public string? FullContentHtml { get; set; }
    public List<YardimMerkeziFaqItemViewModel> FaqItems { get; set; } = new();
}

public class YardimMerkeziFaqItemViewModel
{
    public string Question { get; set; } = string.Empty;
    public string AnswerHtml { get; set; } = string.Empty;
}

public class YardimMerkeziKategoriDetaySayfaViewModel
{
    public DestekKategoriViewModel Category { get; set; } = new();
    public List<DestekMakaleViewModel> RelatedArticles { get; set; } = new();
    public string SearchTerm { get; set; } = string.Empty;
}

public class YardimMerkeziIcerikSayfaViewModel
{
    public string ContentType { get; set; } = "about";
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? HeroTitle { get; set; }
    public string? HeroSubtitle { get; set; }
    public string? HeroImageUrl { get; set; }
    public string Html { get; set; } = string.Empty;
}

public class DestekMakaleViewModel
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Html { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-circle-question";
    public string LinkUrl { get; set; } = "#";
}

public class DestekKanalViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-headset";
    public string ButtonText { get; set; } = string.Empty;
    public string Url { get; set; } = "#";
    public string Note { get; set; } = string.Empty;
    public string Tone { get; set; } = "primary";
}

public class SssKategoriViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class SssBolumViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-circle-question";
    public List<SssSoruViewModel> Questions { get; set; } = new();
}

public class SssSoruViewModel
{
    public long Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}
