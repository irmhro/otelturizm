namespace otelturizmnew.Models.Paneller.Admin;

public class AdminPuanYonetimiPageViewModel
{
    public AdminShellViewModel Shell { get; set; } = new();
    public string ActiveTab { get; set; } = "kazanim";
    public List<AdminPuanAyarRowViewModel> KazanimRules { get; set; } = new();
    public List<AdminPuanAyarRowViewModel> IndirimRules { get; set; } = new();
    public AdminPuanAyarForm RuleForm { get; set; } = new();
    public AdminPuanKullaniciAdjustForm AdjustForm { get; set; } = new();
    public List<AdminPuanKullaniciBalanceRowViewModel> UserBalances { get; set; } = new();
    public bool TablesReady { get; set; }
}

public class AdminPuanAyarRowViewModel
{
    public long Id { get; set; }
    public string AyarTipi { get; set; } = "KAZANIM";
    public decimal MinDeger { get; set; }
    public decimal? MaxDeger { get; set; }
    public int? PuanDegeri { get; set; }
    public decimal? IndirimYuzde { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public bool AktifMi { get; set; }
    public int SiraNo { get; set; }
    public string OlusturulmaTarihiText { get; set; } = string.Empty;

    public string RangeText => MaxDeger.HasValue
        ? $"{MinDeger:N0} – {MaxDeger.Value:N0}"
        : $"{MinDeger:N0}+";

    public string ValueText => string.Equals(AyarTipi, "INDIRIM", StringComparison.OrdinalIgnoreCase)
        ? IndirimYuzde.HasValue ? $"%{IndirimYuzde.Value:N0}" : "—"
        : PuanDegeri.HasValue ? $"{PuanDegeri.Value} puan" : "—";
}

public class AdminPuanAyarForm
{
    public long? Id { get; set; }
    public string AyarTipi { get; set; } = "KAZANIM";
    public decimal MinDeger { get; set; }
    public decimal? MaxDeger { get; set; }
    public int? PuanDegeri { get; set; }
    public decimal? IndirimYuzde { get; set; }
    public string Aciklama { get; set; } = string.Empty;
    public bool AktifMi { get; set; } = true;
    public int SiraNo { get; set; } = 100;
}

public class AdminPuanKullaniciAdjustForm
{
    public long UserId { get; set; }
    public long HotelId { get; set; }
    public int PointDelta { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class AdminPuanKullaniciBalanceRowViewModel
{
    public long UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public long HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public int AvailablePoints { get; set; }
    public int TotalEarned { get; set; }
    public int UsedPoints { get; set; }
}
