using Optimizer.App.Services;
using Xunit;

namespace Optimizer.Windows.Tests;

/// <summary>
/// Windows integration tests: these intentionally invoke real Windows APIs.
/// Run this project on Windows; they do not turn an unsupported platform into a pass.
/// </summary>
public sealed class WindowsDiagnosticServiceTests
{
    [Fact]
    public void GetHardwareInfo_ReadsSystemMemoryAndReadableLabels()
    {
        var service = new WindowsDiagnosticService();

        var hardware = service.GetHardwareInfo();

        Assert.Contains("Windows", hardware.OperatingSystem, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(hardware.ProcessorName));
        Assert.False(string.IsNullOrWhiteSpace(hardware.GraphicsCardName));
        Assert.True(hardware.TotalMemoryBytes > 0);
    }

    [Fact]
    public async Task CaptureSnapshot_AfterSamplingIntervalReturnsBoundedCountersAndSortedProcesses()
    {
        var service = new WindowsDiagnosticService();
        var first = service.CaptureSnapshot();

        await Task.Delay(TimeSpan.FromMilliseconds(300));
        var snapshot = service.CaptureSnapshot();

        Assert.Equal(0d, first.CpuUsagePercent);
        Assert.True(double.IsFinite(snapshot.CpuUsagePercent));
        Assert.InRange(snapshot.CpuUsagePercent, 0d, 100d);
        Assert.True(snapshot.TotalMemoryBytes > 0);
        Assert.InRange(snapshot.AvailableMemoryBytes, 0UL, snapshot.TotalMemoryBytes);
        Assert.InRange(snapshot.UsedMemoryBytes, 0UL, snapshot.TotalMemoryBytes);
        Assert.True(double.IsFinite(snapshot.MemoryUsagePercent));
        Assert.InRange(snapshot.MemoryUsagePercent, 0d, 100d);
        Assert.InRange(snapshot.Processes.Count, 1, 8);
        Assert.Equal(snapshot.Processes.Count, snapshot.Processes.Select(process => process.Id).Distinct().Count());

        foreach (var process in snapshot.Processes)
        {
            Assert.True(process.Id > 0);
            Assert.False(string.IsNullOrWhiteSpace(process.Name));
            Assert.True(process.WorkingSetBytes >= 0);
            if (process.CpuPercent is double cpu)
            {
                Assert.True(double.IsFinite(cpu));
                Assert.InRange(cpu, 0d, 100d);
            }
        }

        for (var index = 1; index < snapshot.Processes.Count; index++)
        {
            var previous = snapshot.Processes[index - 1];
            var current = snapshot.Processes[index];
            var previousCpu = previous.CpuPercent ?? 0d;
            var currentCpu = current.CpuPercent ?? 0d;
            Assert.True(previousCpu >= currentCpu, "Processes should be ordered by descending CPU usage.");
            if (previousCpu == currentCpu)
            {
                Assert.True(previous.WorkingSetBytes >= current.WorkingSetBytes,
                    "Equal CPU usage should be ordered by descending memory usage.");
            }
        }
    }
}
