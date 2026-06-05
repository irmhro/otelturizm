namespace otelturizmnew.Services;

public static class DawnSurprisePricing
{
    public static bool TryApplyPercent(ref decimal totalAmount, ref decimal netRoomAmount, ref decimal vatAmount, ref decimal accommodationTaxAmount, ref decimal taxAmount, int percent)
    {
        if (percent < 3 || percent > 10 || totalAmount <= 0m)
        {
            return false;
        }

        var discountAmount = Math.Round(totalAmount * percent / 100m, 2, MidpointRounding.AwayFromZero);
        if (discountAmount <= 0m)
        {
            return false;
        }

        var newTotal = totalAmount - discountAmount;
        if (newTotal <= 0m)
        {
            return false;
        }

        var scale = newTotal / totalAmount;
        totalAmount = newTotal;
        netRoomAmount = Math.Round(netRoomAmount * scale, 2, MidpointRounding.AwayFromZero);
        vatAmount = Math.Round(vatAmount * scale, 2, MidpointRounding.AwayFromZero);
        accommodationTaxAmount = Math.Max(0m, newTotal - netRoomAmount - vatAmount);
        taxAmount = vatAmount + accommodationTaxAmount;
        return true;
    }
}
