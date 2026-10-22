namespace Optimizer.Core;

public sealed record SystemSnapshot(
    DateTimeOffset CapturedAt,
    double CpuUsagePercent,
    ulong TotalMemoryBytes,
    ulong AvailableMemoryBytes,
    IReadOnlyList<ProcessSnapshot> Processes)
{
    public ulong UsedMemoryBytes => TotalMemoryBytes - Math.Min(AvailableMemoryBytes, TotalMemoryBytes);

    public double MemoryUsagePercent => TotalMemoryBytes == 0
        ? 0
        : (double)UsedMemoryBytes / TotalMemoryBytes * 100;
}
