namespace VoucherMgt.Common;

public static class Money
{
    public const string Ugx = "UGX";
    public const string Usd = "USD";

    public static bool IsSupported(string? currency) =>
        string.Equals(currency, Ugx, StringComparison.OrdinalIgnoreCase)
        || string.Equals(currency, Usd, StringComparison.OrdinalIgnoreCase);

    public static string Normalize(string? currency) =>
        string.Equals(currency, Usd, StringComparison.OrdinalIgnoreCase) ? Usd : Ugx;

    public static decimal Round(decimal value, string currency) =>
        Normalize(currency) == Usd
            ? Math.Round(value, 2, MidpointRounding.AwayFromZero)
            : Math.Round(value, 0, MidpointRounding.AwayFromZero);

    public static string FormatNumber(decimal amount, string currency) =>
        Normalize(currency) == Usd
            ? amount.ToString("#,##0.00")
            : amount.ToString("#,##0");

    public static string Format(decimal amount, string currency) =>
        Normalize(currency) == Usd
            ? "$" + FormatNumber(amount, Usd)
            : "UGX " + FormatNumber(amount, Ugx);

    public static string FormatPair(decimal ugx, decimal usd)
    {
        var hasUgx = ugx != 0;
        var hasUsd = usd != 0;
        if (hasUgx && hasUsd)
        {
            return $"{Format(ugx, Ugx)} + {Format(usd, Usd)}";
        }

        return hasUsd ? Format(usd, Usd) : Format(ugx, Ugx);
    }
}

public static class VatCalculator
{
    public static (decimal Net, decimal Vat, decimal Gross) Calculate(decimal lineSum, VatMode mode, decimal ratePercent, string currency)
    {
        lineSum = Money.Round(lineSum, currency);
        if (mode == VatMode.Exempt || ratePercent <= 0)
        {
            return (lineSum, 0, lineSum);
        }

        if (mode == VatMode.Inclusive)
        {
            var net = Money.Round(lineSum / (1 + (ratePercent / 100m)), currency);
            return (net, Money.Round(lineSum - net, currency), lineSum);
        }

        var vat = Money.Round(lineSum * ratePercent / 100m, currency);
        return (lineSum, vat, Money.Round(lineSum + vat, currency));
    }
}
