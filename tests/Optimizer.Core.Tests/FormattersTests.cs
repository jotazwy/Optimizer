using Optimizer.Core;
using Xunit;

namespace Optimizer.Core.Tests;

public sealed class FormattersTests
{
    [Theory]
    [InlineData(0UL, "0 B")]
    [InlineData(1_023UL, "1023 B")]
    [InlineData(1_024UL, "1 KiB")]
    [InlineData(1_610_612_736UL, "1,5 GiB")]
    public void BytesUsesBinaryUnitsAndPortugueseDecimalSeparator(ulong bytes, string expected) =>
        Assert.Equal(expected, Formatters.Bytes(bytes));

    [Theory]
    [InlineData(null, "—")]
    [InlineData(double.NaN, "—")]
    [InlineData(double.PositiveInfinity, "—")]
    [InlineData(12.34, "12,3%")]
    [InlineData(-1d, "0,0%")]
    [InlineData(101d, "100,0%")]
    public void PercentHandlesUnavailableAndOutOfRangeReadings(double? value, string expected) =>
        Assert.Equal(expected, Formatters.Percent(value));
}
