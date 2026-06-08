namespace otelturizmnew.Utils;

/// <summary>Liste/detay sadakat rozeti — rezervasyon başına sabit puan (gelecekte fiyat bazlı genişletilebilir).</summary>
public static class LoyaltyPointsEstimator
{
    public const int ReservationPreviewPoints = 4;

    public static int? EstimateFromNightlyPrice(decimal? nightlyPrice)
    {
        if (nightlyPrice is not > 0m)
        {
            return null;
        }

        return ReservationPreviewPoints;
    }
}
