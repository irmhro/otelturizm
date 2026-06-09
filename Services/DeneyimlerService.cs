using otelturizmnew.Models.Deneyimler;
using otelturizmnew.Services.Abstractions;

namespace otelturizmnew.Services;

public class DeneyimlerService : IDeneyimlerService
{
    public DeneyimlerPageViewModel GetPage()
    {
        return new DeneyimlerPageViewModel
        {
            HeroEyebrow = "Otelturizm Deneyim Atölyesi",
            HeroTitle = "Konaklamayı anıya dönüştüren rotalar",
            HeroLead = "Şehir kaçamaklarından spa ritüellerine — ruh halinize uygun otelleri Akıllı Rota etiketleriyle keşfedin, koleksiyonlardan ilham alın.",
            Stats =
            [
                new() { Value = "50+", Label = "Akıllı rota etiketi" },
                new() { Value = "48", Label = "Şehir & bölge" },
                new() { Value = "4,8", Label = "Misafir puanı" }
            ],
            LiveTicker =
            [
                "🌅 Kapadokya safak balonu — bu hafta en çok aranan",
                "🫧 Antalya thalasso günü — spa otelleri yükselişte",
                "👨‍👩‍👧 Aile dostu hafta sonu — Uludağ & Bodrum",
                "🍽️ Gurme rotası — İzmir & İstanbul seçkisi",
                "🌊 Denize sıfır kaçamak — Ege koyları"
            ],
            MoodPicks =
            [
                new() { CategoryKey = "sehir", Label = "Şehir keşfi", Emoji = "🏙️", Hint = "Müze, teras, gece yürüyüşü" },
                new() { CategoryKey = "sahil", Label = "Sahil molası", Emoji = "🌊", Hint = "Koy, marina, gün batımı" },
                new() { CategoryKey = "spa", Label = "Spa & dinlenme", Emoji = "🫧", Hint = "Thalasso, masaj, sessizlik" },
                new() { CategoryKey = "gastronomi", Label = "Lezzet turu", Emoji = "🍽️", Hint = "Yerel tat, fine dining" },
                new() { CategoryKey = "aile", Label = "Aile kaçamağı", Emoji = "👨‍👩‍👧", Hint = "Çocuk kulübü, kayak, havuz" },
                new() { CategoryKey = "macera", Label = "Macera", Emoji = "⛰️", Hint = "Balon, yayla, doğa yürüyüşü" }
            ],
            Spotlight = new DeneyimSpotlightViewModel
            {
                Badge = "Haftanın rotası",
                Title = "Kapadokya safak + mağara konaklama",
                Subtitle = "Gün doğumunda balon, akşam mağara suit — 2 gece önerilen paket akışı",
                Location = "Nevşehir",
                Duration = "2 gece",
                LinkUrl = "/oteller?q=Kapadokya",
                Accent = "#FF385C",
                Emoji = "🎈"
            },
            SmartRoutes = BuildSmartRoutes(),
            CampaignHighlights =
            [
                new() { Title = "Şehir Otelleri Kampanyası", Tag = "−%15'e varan", LinkUrl = "/kampanyalar" },
                new() { Title = "Hafta Sonu Kaçamakları", Tag = "Club özel", LinkUrl = "/oteller?etiket=hafta-sonu-firsatlari" },
                new() { Title = "Spa & Wellness Otelleri", Tag = "Wellness", LinkUrl = "/oteller?q=spa+wellness" }
            ],
            Categories =
            [
                new() { Key = "all", Label = "Tümü", Icon = "fa-layer-group", Emoji = "✨" },
                new() { Key = "sehir", Label = "Şehir", Icon = "fa-city", Emoji = "🏙️" },
                new() { Key = "sahil", Label = "Sahil", Icon = "fa-umbrella-beach", Emoji = "🌊" },
                new() { Key = "spa", Label = "Spa & Wellness", Icon = "fa-spa", Emoji = "🫧" },
                new() { Key = "gastronomi", Label = "Gastronomi", Icon = "fa-utensils", Emoji = "🍽️" },
                new() { Key = "aile", Label = "Aile", Icon = "fa-people-roof", Emoji = "👨‍👩‍👧" },
                new() { Key = "macera", Label = "Macera", Icon = "fa-person-hiking", Emoji = "⛰️" }
            ],
            Featured =
            [
                new()
                {
                    Key = "istanbul-gece",
                    CategoryKey = "sehir",
                    Title = "İstanbul'da gece ışıkları rotası",
                    Subtitle = "Teras kahvesi, Boğaz silueti ve gece yürüyüşü",
                    Mood = "Romantik · Urban",
                    Location = "İstanbul",
                    Duration = "1 gece",
                    Stamp = "EDITÖR SEÇİMİ",
                    Accent = "#E30A17",
                    Gradient = "linear-gradient(160deg, #1a1919 0%, #4a0404 42%, #003b95 100%)",
                    LinkUrl = "/oteller?q=İstanbul",
                    IsFeatured = true,
                    IsWide = true
                },
                new()
                {
                    Key = "kapadokya-safak",
                    CategoryKey = "macera",
                    Title = "Kapadokya safak balonu",
                    Subtitle = "Gün doğumunda vadiler üzerinde sessiz uçuş",
                    Mood = "Macera · Sessiz",
                    Location = "Nevşehir",
                    Duration = "Yarım gün",
                    Stamp = "YENİ",
                    Accent = "#FF385C",
                    Gradient = "linear-gradient(145deg, #2d1b4e 0%, #ff6b6b 55%, #ffd166 100%)",
                    LinkUrl = "/oteller?q=Kapadokya",
                    IsFeatured = true
                },
                new()
                {
                    Key = "antalya-spa",
                    CategoryKey = "spa",
                    Title = "Akdeniz spa günü",
                    Subtitle = "Thalasso ritüeli ve gün batımı terası",
                    Mood = "Dinlenme · Lüks",
                    Location = "Antalya",
                    Duration = "1 gün",
                    Stamp = "RAHATLAMA",
                    Accent = "#0ea5e9",
                    Gradient = "linear-gradient(145deg, #0c4a6e 0%, #38bdf8 50%, #fef3c7 100%)",
                    LinkUrl = "/oteller?q=Antalya",
                    IsFeatured = true
                }
            ],
            Collections =
            [
                new()
                {
                    Key = "ege-koy",
                    CategoryKey = "sahil",
                    Title = "Ege koylarında yavaş tempo",
                    Subtitle = "Tekne molası, koy yüzme ve gün batımı",
                    Mood = "Sakin · Mavi",
                    Location = "Muğla",
                    Duration = "2-3 gün",
                    Stamp = "SAHİL",
                    Accent = "#0284c7",
                    Gradient = "linear-gradient(135deg, #0369a1 0%, #7dd3fc 100%)",
                    LinkUrl = "/oteller?q=Bodrum"
                },
                new()
                {
                    Key = "izmir-gastronomi",
                    CategoryKey = "gastronomi",
                    Title = "İzmir sokak lezzetleri",
                    Subtitle = "Kemeraltı turu ve kordon akşam yemeği",
                    Mood = "Lezzet · Yerel",
                    Location = "İzmir",
                    Duration = "1 gün",
                    Stamp = "TADIM",
                    Accent = "#ea580c",
                    Gradient = "linear-gradient(135deg, #7c2d12 0%, #fb923c 100%)",
                    LinkUrl = "/oteller?q=İzmir"
                },
                new()
                {
                    Key = "aile-kayak",
                    CategoryKey = "aile",
                    Title = "Aile kayak hafta sonu",
                    Subtitle = "Çocuk okulu, sıcak çikolata molası",
                    Mood = "Aile · Kış",
                    Location = "Uludağ",
                    Duration = "2 gece",
                    Stamp = "AİLE",
                    Accent = "#6366f1",
                    Gradient = "linear-gradient(135deg, #312e81 0%, #e0e7ff 100%)",
                    LinkUrl = "/oteller?q=Uludağ"
                },
                new()
                {
                    Key = "ankara-kultur",
                    CategoryKey = "sehir",
                    Title = "Ankara kültür rotası",
                    Subtitle = "Müze sabahı, Anıtkabir ve akşam caz",
                    Mood = "Kültür · Klasik",
                    Location = "Ankara",
                    Duration = "1 gün",
                    Stamp = "KEŞİF",
                    Accent = "#64748b",
                    Gradient = "linear-gradient(135deg, #1e293b 0%, #94a3b8 100%)",
                    LinkUrl = "/oteller?q=Ankara"
                },
                new()
                {
                    Key = "trabzon-yayla",
                    CategoryKey = "macera",
                    Title = "Karadeniz yayla nefesi",
                    Subtitle = "Sisli yürüyüş ve yerel kahvaltı",
                    Mood = "Doğa · Serin",
                    Location = "Trabzon",
                    Duration = "2 gün",
                    Stamp = "YAYLA",
                    Accent = "#16a34a",
                    Gradient = "linear-gradient(135deg, #14532d 0%, #86efac 100%)",
                    LinkUrl = "/oteller?q=Trabzon"
                },
                new()
                {
                    Key = "marmaris-gece",
                    CategoryKey = "sahil",
                    Title = "Marmaris gece limanı",
                    Subtitle = "Marina yürüyüşü ve canlı müzik",
                    Mood = "Eğlence · Yaz",
                    Location = "Muğla",
                    Duration = "1 gece",
                    Stamp = "GECE",
                    Accent = "#db2777",
                    Gradient = "linear-gradient(135deg, #831843 0%, #f472b6 100%)",
                    LinkUrl = "/oteller?q=Marmaris"
                },
                new()
                {
                    Key = "bursa-termal",
                    CategoryKey = "spa",
                    Title = "Bursa termal hafta sonu",
                    Subtitle = "Hamam ritüeli ve osmanlı çarşı turu",
                    Mood = "Termal · Kültür",
                    Location = "Bursa",
                    Duration = "2 gece",
                    Stamp = "TERMAL",
                    Accent = "#0891b2",
                    Gradient = "linear-gradient(135deg, #164e63 0%, #67e8f9 100%)",
                    LinkUrl = "/oteller?q=Bursa+termal"
                },
                new()
                {
                    Key = "fethiye-dalis",
                    CategoryKey = "macera",
                    Title = "Fethiye mavi yolculuk",
                    Subtitle = "Koy atlama, dalış ve gün batımı",
                    Mood = "Macera · Deniz",
                    Location = "Muğla",
                    Duration = "3 gün",
                    Stamp = "DENİZ",
                    Accent = "#2563eb",
                    Gradient = "linear-gradient(135deg, #1e3a8a 0%, #93c5fd 100%)",
                    LinkUrl = "/oteller?q=Fethiye"
                }
            ],
            Stories =
            [
                new()
                {
                    Title = "Sabah 06:12",
                    Quote = "Balon sepetinde sessizlik vardı; sadece fotoğraf değil, nefes aldım.",
                    Author = "Elif K.",
                    Location = "Kapadokya",
                    Accent = "#FF385C"
                },
                new()
                {
                    Title = "Akşam 19:40",
                    Quote = "Otel terasında gün batımı çayı — rezervasyon sonrası en güzel anımız bu oldu.",
                    Author = "Murat & Selin",
                    Location = "Kaş",
                    Accent = "#0ea5e9"
                },
                new()
                {
                    Title = "Öğle 13:05",
                    Quote = "Çocuklar kayak okulundayken biz spa'da 45 dakikalık kaçış yaptık.",
                    Author = "Ayşe T.",
                    Location = "Uludağ",
                    Accent = "#6366f1"
                }
            ],
            JourneySteps =
            [
                new() { Step = 1, Title = "Temayı seç", Text = "Şehir, sahil veya wellness — ruh halinize uygun koleksiyonu keşfedin.", Icon = "fa-compass" },
                new() { Step = 2, Title = "Konaklamayı eşle", Text = "Deneyime yakın otelleri filtreleyin; tek akışta rezervasyon oluşturun.", Icon = "fa-hotel" },
                new() { Step = 3, Title = "Anıyı yaşayın", Text = "Check-in sonrası önerilen rotalar ve yerel ipuçlarıyla yolculuğunuzu tamamlayın.", Icon = "fa-star" }
            ]
        };
    }

    private static List<DeneyimSmartRouteViewModel> BuildSmartRoutes()
    {
        (string hashtag, string query, string color)[] tags =
        [
            ("#SakinlikArayanlar", "sakin huzurlu ortam", "sage"),
            ("#ÇocukDostu", "çocuk dostu aile oteli", "amber"),
            ("#GurmeDeneyimi", "gurme fine dining", "wine"),
            ("#ÖzelHavuzlu", "özel havuz suite", "sky"),
            ("#DenizeSıfır", "denize sıfır plaj", "ocean"),
            ("#SpaWellness", "spa wellness", "violet"),
            ("#Balayı", "balayı romantik", "wine"),
            ("#İşSeyahati", "iş seyahati merkezi", "sage"),
            ("#PetDostu", "evcil hayvan dostu", "amber"),
            ("#ButikOtel", "butik otel", "sky"),
            ("#TermalRahat", "termal kaplıca", "violet"),
            ("#KayakKış", "kayak kış oteli", "ocean")
        ];

        return tags.Select(t => new DeneyimSmartRouteViewModel
        {
            Hashtag = t.hashtag,
            SearchQuery = t.query,
            ColorClass = t.color,
            LinkUrl = $"/oteller?q={Uri.EscapeDataString(t.query)}"
        }).ToList();
    }
}
