namespace otelturizmnew.Utils;

/// <summary>Liste/detay sadakat rozeti için gecelik fiyat tahmini (1 puan ≈ 25 TL).</summary>
public static class LoyaltyPointsEstimator
{
    public static int? EstimateFromNightlyPrice(decimal? nightlyPrice)
    {
        if (nightlyPrice is not > 0m)
        {
            return null;
        }

        return Math.Max(1, (int)Math.Floor(nightlyPrice.Value / 25m));
    }
}
