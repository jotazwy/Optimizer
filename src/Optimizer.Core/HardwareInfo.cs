namespace Optimizer.Core;

public sealed record HardwareInfo(
    string OperatingSystem,
    string ProcessorName,
    string GraphicsCardName,
    ulong TotalMemoryBytes);
