namespace otelturizmnew.Models.Firma;

public class FirmaLandingPageViewModel
{
    public string HeroTitle { get; set; } = "Firmanız İçin Toplu Konaklama Avantajları";
    public string HeroDescription { get; set; } = string.Empty;
    public int ActiveCompanyCount { get; set; }
    public int ContractedHotelCount { get; set; }
    public decimal MaxDiscountRate { get; set; }
    public List<FirmaLandingStatViewModel> HeroStats { get; set; } = new();
    public List<FirmaLandingDealViewModel> FeaturedDeals { get; set; } = new();
    public List<FirmaLandingTierViewModel> PricingTiers { get; set; } = new();
    public List<FirmaLandingBenefitViewModel> Benefits { get; set; } = new();
}

public class FirmaLandingStatViewModel
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class FirmaLandingDealViewModel
{
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string StandardPriceText { get; set; } = string.Empty;
    public string CorporatePriceText { get; set; } = string.Empty;
    public string DiscountRateText { get; set; } = string.Empty;
    public string MinRoomText { get; set; } = string.Empty;
    public string SavingsText { get; set; } = string.Empty;
}

public class FirmaLandingTierViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Range { get; set; } = string.Empty;
    public string DiscountText { get; set; } = string.Empty;
    public string ExampleText { get; set; } = string.Empty;
    public bool Highlighted { get; set; }
}

public class FirmaLandingBenefitViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-building";
}
