namespace otelturizmnew.Models.Paneller.Admin;

public class AdminOzelGunlerPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public List<AdminOzelGunRowViewModel> Rows { get; set; } = new();
    public AdminOzelGunForm Form { get; set; } = new();
    public int TotalCount { get; set; }
}

public class AdminOzelGunRowViewModel
{
    public int Id { get; set; }
    public string GunKodu { get; set; } = string.Empty;
    public string GunAdi { get; set; } = string.Empty;
    public byte Ay { get; set; }
    public byte? Gun { get; set; }
    public string KuralTipi { get; set; } = "SABIT";
    public byte? KuralParam1 { get; set; }
    public byte? KuralParam2 { get; set; }
    public string Emoji { get; set; } = string.Empty;
    public string KutlamaMetni { get; set; } = string.Empty;
    public string Kategori { get; set; } = string.Empty;
    public bool AktifMi { get; set; }
    public int Siralama { get; set; }
    public string OlusturulmaTarihiText { get; set; } = string.Empty;

    public string AyText => Ay switch
    {
        1 => "Ocak",
        2 => "Şubat",
        3 => "Mart",
        4 => "Nisan",
        5 => "Mayıs",
        6 => "Haziran",
        7 => "Temmuz",
        8 => "Ağustos",
        9 => "Eylül",
        10 => "Ekim",
        11 => "Kasım",
        12 => "Aralık",
        _ => Ay.ToString()
    };

    public string KuralText => string.Equals(KuralTipi, "NINCI_HAFTA_GUNU", StringComparison.OrdinalIgnoreCase)
        ? $"{KuralParam1 ?? 1}. hafta · {WeekdayText(KuralParam2)}"
        : Gun.HasValue ? $"{Gun.Value}. gün" : "-";

    private static string WeekdayText(byte? value) => value switch
    {
        0 => "Pazar",
        1 => "Pazartesi",
        2 => "Salı",
        3 => "Çarşamba",
        4 => "Perşembe",
        5 => "Cuma",
        6 => "Cumartesi",
        _ => "-"
    };
}

public class AdminOzelGunForm
{
    public int? Id { get; set; }
    public string GunKodu { get; set; } = string.Empty;
    public string GunAdi { get; set; } = string.Empty;
    public byte Ay { get; set; } = 1;
    public byte? Gun { get; set; }
    public string KuralTipi { get; set; } = "SABIT";
    public byte? KuralParam1 { get; set; }
    public byte? KuralParam2 { get; set; }
    public string Emoji { get; set; } = string.Empty;
    public string KutlamaMetni { get; set; } = string.Empty;
    public string Kategori { get; set; } = string.Empty;
    public bool AktifMi { get; set; } = true;
    public int Siralama { get; set; } = 100;
}
