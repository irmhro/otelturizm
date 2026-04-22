namespace otelturizmnew.Pricing;

/// <summary>
/// Veritabaninda gece fiyati KDV ve konaklama vergisi <b>onarinda</b> (net oda tutari) saklanir.
/// Partner panelinde girilen tutar misafirin gordugu vergi dahil tek tutardir.
/// </summary>
public static class InclusiveNightlyPricing
{
    public static decimal PartnerGrossEntryToStoredNet(decimal grossInclusive, decimal vatPercent, decimal accommodationTaxPercent)
    {
        if (grossInclusive <= 0m)
        {
            return 0m;
        }

        var divisor = 1m + vatPercent / 100m + accommodationTaxPercent / 100m;
        if (divisor <= 0m)
        {
            return decimal.Round(grossInclusive, 2, MidpointRounding.AwayFromZero);
        }

        return decimal.Round(grossInclusive / divisor, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal StoredNetToGuestDisplay(decimal storedNet, decimal vatPercent, decimal accommodationTaxPercent)
    {
        if (storedNet <= 0m)
        {
            return 0m;
        }

        return decimal.Round(
            storedNet * (1m + vatPercent / 100m + accommodationTaxPercent / 100m),
            0,
            MidpointRounding.AwayFromZero);
    }

    public static decimal StoredNetToPartnerDisplay(decimal storedNet, decimal vatPercent, decimal accommodationTaxPercent)
    {
        if (storedNet <= 0m)
        {
            return 0m;
        }

        return decimal.Round(
            storedNet * (1m + vatPercent / 100m + accommodationTaxPercent / 100m),
            2,
            MidpointRounding.AwayFromZero);
    }
}
