using Optimizer.Core;
using Xunit;

namespace Optimizer.Core.Tests;

public sealed class SystemSnapshotTests
{
    [Fact]
    public void MemoryUsageIsSafeWhenTotalIsUnavailable()
    {
        var snapshot = Snapshot(total: 0, available: 100);

        Assert.Equal(0UL, snapshot.UsedMemoryBytes);
        Assert.Equal(0, snapshot.MemoryUsagePercent);
    }

    [Fact]
    public void AvailableMemoryAboveTotalDoesNotUnderflow()
    {
        var snapshot = Snapshot(total: 1_000, available: 1_001);

        Assert.Equal(0UL, snapshot.UsedMemoryBytes);
        Assert.Equal(0, snapshot.MemoryUsagePercent);
    }

    [Fact]
    public void MemoryPercentageUsesFloatingPointArithmetic()
    {
        var snapshot = Snapshot(total: 1_000, available: 375);

        Assert.Equal(625UL, snapshot.UsedMemoryBytes);
        Assert.Equal(62.5, snapshot.MemoryUsagePercent);
    }

    private static SystemSnapshot Snapshot(ulong total, ulong available) =>
        new(DateTimeOffset.UtcNow, 0, total, available, []);
}
