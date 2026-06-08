namespace otelturizmnew.Utils;

/// <summary>Liste sadakat rozeti — geriye dönük fallback. Tercih: <see cref="Services.Abstractions.IHotelPointsService"/>.</summary>
public static class LoyaltyPointsEstimator
{
    public static int? EstimateFromNightlyPrice(decimal? nightlyPrice)
    {
        if (nightlyPrice is not > 0m)
        {
            return null;
        }

        if (nightlyPrice.Value <= 2000m) return 5;
        if (nightlyPrice.Value <= 5000m) return 10;
        if (nightlyPrice.Value <= 10000m) return 20;
        return 35;
    }
}
