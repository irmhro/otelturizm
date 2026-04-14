namespace otelturizmnew.Models.Anasayfa;

public class AnasayfaViewModel
{
    public string HeroTitle { get; set; } = "Hayalindeki Oteli Kesfet";
    public string HeroDescription { get; set; } = "Turkiye'nin en iyi sehir otelleri, butik tesisleri ve haftasonu kacamaklari tek platformda.";
    public List<HomeHeroSlideViewModel> HeroSlides { get; set; } = new();
    public List<HomeDestinationCardViewModel> PopularDestinations { get; set; } = new();
    public List<HomeHotelCardViewModel> PopularHotels { get; set; } = new();
    public List<HomeHotelCardViewModel> WeekendHotels { get; set; } = new();
}

public class HomeHeroSlideViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string DetailSlug { get; set; } = string.Empty;
    // Optional weather info for the hero slide (icon class & temperature string)
    public string WeatherIcon { get; set; } = string.Empty; // e.g. "fa-cloud-sun"
    public string Temperature { get; set; } = string.Empty; // e.g. "18°C"
}

public class HomeDestinationCardViewModel
{
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public int HotelCount { get; set; }
    public string LeadText { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ListingUrl { get; set; } = string.Empty;
}

public class HomeHotelCardViewModel
{
    public long Id { get; set; }
    public string HotelCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string LocationText { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public string RatingText { get; set; } = "Yorum Bekleniyor";
    public int ReviewCount { get; set; }
    public decimal? StartingPrice { get; set; }
    public string PriceText { get; set; } = "Fiyat icin detay sayfasina bakin";
    public string PriceNote { get; set; } = "Vergiler dahil";
    public string ImageUrl { get; set; } = string.Empty;
    public string DetailSlug { get; set; } = string.Empty;
    public List<HomeAmenityViewModel> Amenities { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public bool IsSmartPrice { get; set; }
    public bool IsFavorite { get; set; }
    // Weather info for display on card
    public string WeatherIcon { get; set; } = ""; // font-awesome icon class e.g. 'fa-cloud-sun'
    public string Temperature { get; set; } = ""; // e.g. '18°C'
}

public class HomeAmenityViewModel
{
    public string Label { get; set; } = string.Empty;
    public string IconClass { get; set; } = "fa-circle-check";
}

