namespace otelturizmnew.Pricing;

/// <summary>
/// Veritabaninda gece fiyati partnerin girdigi vergi dahil brüt tutar olarak saklanir.
/// Vergi kirilimi rezervasyon ve bilgilendirme ekranlarinda bu tutarin icinden ayrilir.
/// </summary>
public static class InclusiveNightlyPricing
{
    private const decimal MaximumExpectedNightlyPrice = 100000m;

    public static decimal PartnerGrossEntryToStoredNet(decimal grossInclusive, decimal vatPercent, decimal accommodationTaxPercent)
    {
        if (grossInclusive <= 0m)
        {
            return 0m;
        }

        return decimal.Round(NormalizeStoredNightlyPrice(grossInclusive), 2, MidpointRounding.AwayFromZero);
    }

    public static decimal StoredNetToGuestDisplay(decimal storedNet, decimal vatPercent, decimal accommodationTaxPercent)
    {
        if (storedNet <= 0m)
        {
            return 0m;
        }

        return decimal.Round(NormalizeStoredNightlyPrice(storedNet), 0, MidpointRounding.AwayFromZero);
    }

    public static decimal StoredNetToPartnerDisplay(decimal storedNet, decimal vatPercent, decimal accommodationTaxPercent)
    {
        if (storedNet <= 0m)
        {
            return 0m;
        }

        return decimal.Round(NormalizeStoredNightlyPrice(storedNet), 2, MidpointRounding.AwayFromZero);
    }

    public static decimal NormalizeStoredNightlyPrice(decimal value)
    {
        if (value <= MaximumExpectedNightlyPrice)
        {
            return value;
        }

        var normalized = value;
        while (normalized > MaximumExpectedNightlyPrice && normalized % 100m == 0m)
        {
            normalized /= 100m;
        }

        return normalized;
    }
}
