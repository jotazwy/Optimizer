using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Optimizer.Core;

namespace Optimizer.App.Services;

/// <summary>
/// Reads Windows counters without requiring administrator privileges or changing the PC.
/// CPU counters need two samples; the first system CPU value is zero and first process
/// CPU values are null. Call again after an interval before interpreting CPU pressure.
/// </summary>
/// <remarks>
/// Windows GetSystemTimes only covers the calling thread's primary processor group.
/// On machines with more than 64 logical processors, the system CPU percentage is
/// therefore not a full-machine measurement. No processor affinity is changed here.
/// </remarks>
public sealed class WindowsDiagnosticService
{
    private const string DisplayAdapterClass =
        @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

    private readonly object _sync = new();
    private Dictionary<int, ProcessReading> _processReadings = new();
    private CpuReading? _previousCpu;

    public HardwareInfo GetHardwareInfo()
    {
        EnsureWindows();
        var memory = ReadMemory();

        return new HardwareInfo(
            RuntimeInformation.OSDescription,
            ReadProcessorName(),
            ReadGraphicsCardNames(),
            memory.TotalPhysical);
    }

    public SystemSnapshot CaptureSnapshot()
    {
        EnsureWindows();

        lock (_sync)
        {
            var memory = ReadMemory();
            var cpuUsage = ReadCpuUsage();
            var processes = ReadProcesses();

            return new SystemSnapshot(
                DateTimeOffset.Now,
                cpuUsage,
                memory.TotalPhysical,
                memory.AvailablePhysical,
                processes);
        }
    }

    private double ReadCpuUsage()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user))
        {
            throw CounterException("Não foi possível consultar o uso da CPU");
        }

        var current = new CpuReading(idle.Value, kernel.Value + user.Value);
        var previous = _previousCpu;
        _previousCpu = current;

        if (previous is null || current.Total <= previous.Value.Total ||
            current.Idle < previous.Value.Idle)
        {
            return 0;
        }

        var totalDelta = current.Total - previous.Value.Total;
        var idleDelta = current.Idle - previous.Value.Idle;
        return Math.Clamp(100d * (1d - (double)idleDelta / totalDelta), 0d, 100d);
    }

    private IReadOnlyList<ProcessSnapshot> ReadProcesses()
    {
        var snapshots = new List<ProcessSnapshot>();
        var readings = new Dictionary<int, ProcessReading>();

        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    var id = process.Id;
                    if (id == 0)
                    {
                        // The idle process represents spare capacity, not CPU pressure.
                        continue;
                    }

                    var name = process.ProcessName;
                    var memory = process.WorkingSet64;
                    double? cpu = null;

                    try
                    {
                        // Start time prevents comparing counters belonging to different
                        // processes when Windows reuses a PID between refreshes.
                        var startTime = process.StartTime.ToUniversalTime().Ticks;
                        var processorTime = process.TotalProcessorTime.Ticks;
                        var timestamp = Stopwatch.GetTimestamp();
                        var reading = new ProcessReading(startTime, processorTime, timestamp);
                        readings[id] = reading;

                        if (_processReadings.TryGetValue(id, out var previous) &&
                            previous.StartTime == startTime &&
                            processorTime >= previous.ProcessorTime && timestamp > previous.Timestamp)
                        {
                            var elapsedSeconds = (double)(timestamp - previous.Timestamp) /
                                Stopwatch.Frequency;
                            var processorSeconds = (double)(processorTime - previous.ProcessorTime) /
                                TimeSpan.TicksPerSecond;
                            cpu = Math.Clamp(
                                processorSeconds / elapsedSeconds / Environment.ProcessorCount * 100d,
                                0d,
                                100d);
                        }
                    }
                    catch (Exception exception) when (IsUnavailableProcess(exception))
                    {
                        // Access to CPU counters can be denied even when memory is readable.
                    }

                    snapshots.Add(new ProcessSnapshot(id, name, cpu, memory));
                }
                catch (Exception exception) when (IsUnavailableProcess(exception))
                {
                    // A process may exit or become inaccessible at any point in enumeration.
                }
            }
        }

        _processReadings = readings;
        return snapshots
            .OrderByDescending(process => process.CpuPercent ?? 0d)
            .ThenByDescending(process => process.WorkingSetBytes)
            .Take(8)
            .ToArray();
    }

    private static MemoryStatus ReadMemory()
    {
        var memory = new MemoryStatus
        {
            Length = (uint)Marshal.SizeOf<MemoryStatus>()
        };

        if (!GlobalMemoryStatusEx(ref memory))
        {
            throw CounterException("Não foi possível consultar a memória do sistema");
        }

        return memory;
    }

    private static string ReadProcessorName()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            var name = key?.GetValue("ProcessorNameString") as string;
            return string.IsNullOrWhiteSpace(name)
                ? "Processador não identificado"
                : name.Trim();
        }
        catch (Exception exception) when (IsUnavailableRegistry(exception))
        {
            return "Processador não identificado (leitura indisponível)";
        }
    }

    private static string ReadGraphicsCardNames()
    {
        try
        {
            using var adapters = Registry.LocalMachine.OpenSubKey(DisplayAdapterClass);
            if (adapters is null)
            {
                return "Placa de vídeo não identificada";
            }

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var subKeyName in adapters.GetSubKeyNames())
            {
                if (subKeyName.Length != 4 || !subKeyName.All(char.IsDigit))
                {
                    continue;
                }

                using var adapter = adapters.OpenSubKey(subKeyName);
                if (adapter?.GetValue("DriverDesc") is string name &&
                    !string.IsNullOrWhiteSpace(name))
                {
                    names.Add(name.Trim());
                }
            }

            return names.Count > 0
                ? string.Join(" / ", names.OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
                : "Placa de vídeo não identificada";
        }
        catch (Exception exception) when (IsUnavailableRegistry(exception))
        {
            return "Placa de vídeo não identificada (leitura indisponível)";
        }
    }

    private static Win32Exception CounterException(string message) =>
        new(Marshal.GetLastWin32Error(), message);

    private static bool IsUnavailableProcess(Exception exception) =>
        exception is Win32Exception or InvalidOperationException or NotSupportedException
            or UnauthorizedAccessException or System.Security.SecurityException;

    private static bool IsUnavailableRegistry(Exception exception) =>
        exception is UnauthorizedAccessException or System.Security.SecurityException
            or IOException;

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("O diagnóstico requer Windows 10 ou 11.");
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(
        out NativeFileTime idleTime,
        out NativeFileTime kernelTime,
        out NativeFileTime userTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatus memoryStatus);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeFileTime
    {
        public uint Low;
        public uint High;
        public readonly ulong Value => ((ulong)High << 32) | Low;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatus
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    private readonly record struct CpuReading(ulong Idle, ulong Total);
    private readonly record struct ProcessReading(long StartTime, long ProcessorTime, long Timestamp);
}
