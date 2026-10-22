using System.Globalization;

namespace Optimizer.Core;

public static class Formatters
{
    private static readonly CultureInfo Portuguese = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly string[] ByteUnits = ["B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB"];

    public static string Bytes(ulong bytes)
    {
        double value = bytes;
        var unit = 0;

        while (value >= 1024 && unit < ByteUnits.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value.ToString(unit == 0 ? "0" : "0.##", Portuguese)} {ByteUnits[unit]}";
    }

    public static string Percent(double? value) => value is double percentage && double.IsFinite(percentage)
        ? $"{Math.Clamp(percentage, 0, 100).ToString("0.0", Portuguese)}%"
        : "—";
}
