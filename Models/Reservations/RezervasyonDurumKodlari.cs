namespace otelturizmnew.Models.Reservations;

/// <summary>
/// Stabil rezervasyon durum kodları — veritabanında <c>rezervasyon_durum_tanimlari.kod</c> ile eşleşir.
/// </summary>
public static class RezervasyonDurumKodlari
{
    public const string OnayBekliyor = "ONAY_BEKLIYOR";
    public const string Onaylandi = "ONAYLANDI";
    public const string IptalEdildi = "IPTAL_EDILDI";
    public const string NoShow = "NO_SHOW";
    public const string Tamamlandi = "TAMAMLANDI";
    public const string DegisiklikBekliyor = "DEGISIKLIK_BEKLIYOR";
}
