using System.Globalization;

namespace EcwidSync.Modules.Suppliers.Parsing;

internal static class ParseUtil
{
    private static readonly CultureInfo Pt = new("pt-PT");
    private static readonly CultureInfo En = CultureInfo.InvariantCulture;

    public static decimal? TryDecimal(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim().Trim('"', '\'');
        // troca vírgula por ponto se necessário
        if (decimal.TryParse(s, NumberStyles.Any, Pt, out var d)) return d;
        if (decimal.TryParse(s, NumberStyles.Any, En, out d)) return d;
        s = s.Replace(',', '.');
        if (decimal.TryParse(s, NumberStyles.Any, En, out d)) return d;
        return null;
    }

    public static int? TryInt(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim().Trim('"', '\'');
        if (int.TryParse(s, NumberStyles.Any, Pt, out var v)) return v;
        if (int.TryParse(s, NumberStyles.Any, En, out v)) return v;
        return null;
    }

    public static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
