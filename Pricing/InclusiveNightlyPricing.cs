namespace otelturizmnew.Pricing;

/// <summary>
/// Veritabaninda gece fiyati partnerin girdigi vergi dahil brüt tutar olarak saklanir.
/// Vergi kirilimi rezervasyon ve bilgilendirme ekranlarinda bu tutarin icinden ayrilir.
/// </summary>
public static class InclusiveNightlyPricing
{
    public static decimal PartnerGrossEntryToStoredNet(decimal grossInclusive, decimal vatPercent, decimal accommodationTaxPercent)
    {
        if (grossInclusive <= 0m)
        {
            return 0m;
        }

        return decimal.Round(grossInclusive, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal StoredNetToGuestDisplay(decimal storedNet, decimal vatPercent, decimal accommodationTaxPercent)
    {
        if (storedNet <= 0m)
        {
            return 0m;
        }

        return decimal.Round(storedNet, 0, MidpointRounding.AwayFromZero);
    }

    public static decimal StoredNetToPartnerDisplay(decimal storedNet, decimal vatPercent, decimal accommodationTaxPercent)
    {
        if (storedNet <= 0m)
        {
            return 0m;
        }

        return decimal.Round(storedNet, 2, MidpointRounding.AwayFromZero);
    }
}
