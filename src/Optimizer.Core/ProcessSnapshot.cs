namespace Optimizer.Core;

/// <param name="CpuPercent">
/// Percentage of the whole machine's CPU capacity over a sampling interval.
/// Null means that no comparable previous sample was available.
/// </param>
public sealed record ProcessSnapshot(
    int Id,
    string Name,
    double? CpuPercent,
    long WorkingSetBytes);
