using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using Optimizer.Core;

namespace Optimizer.App;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private const int HistoryCapacity = 30;
    private readonly Queue<(double Cpu, double Memory)> _history = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ProcessorName { get; private set; } = "Identificando…";
    public string GraphicsCardName { get; private set; } = "Identificando…";
    public string OperatingSystem { get; private set; } = "Identificando…";
    public string OperatingSystemName { get; private set; } = "Identificando…";
    public string SystemArchitecture { get; } = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();
    public double CpuPercent { get; private set; }
    public string CpuText { get; private set; } = "—";
    public double MemoryPercent { get; private set; }
    public string MemoryText { get; private set; } = "—";
    public string MemoryDetail { get; private set; } = "Aguardando a primeira leitura.";
    public string MemoryAvailableText { get; private set; } = "Aguardando leitura";
    public PointCollection CpuHistoryPoints { get; private set; } = CreateHistoryPoints([]);
    public PointCollection MemoryHistoryPoints { get; private set; } = CreateHistoryPoints([]);
    public string MonitoringLabel { get; private set; } = "INICIANDO";
    public string MonitoringColor { get; private set; } = "#929EAE";
    public string LastUpdatedText { get; private set; } = "Aguardando leitura";
    public string Status { get; private set; } = "Iniciando diagnóstico…";
    public string StatusColor { get; private set; } = "#929EAE";
    public bool CanRefresh { get; private set; }
    public ObservableCollection<ProcessRow> Processes { get; } = [];
    public ObservableCollection<RecommendationRow> Recommendations { get; } = [];

    public void SetHardware(HardwareInfo hardware)
    {
        ProcessorName = hardware.ProcessorName;
        GraphicsCardName = hardware.GraphicsCardName;
        OperatingSystem = hardware.OperatingSystem;
        OperatingSystemName = System.OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)
            ? "Windows 11"
            : System.OperatingSystem.IsWindows()
                ? "Windows 10"
                : hardware.OperatingSystem;
        NotifyAll();
    }

    public void SetBusy(bool busy)
    {
        CanRefresh = !busy;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanRefresh)));
    }

    public void Apply(SystemSnapshot snapshot, bool warmingUp)
    {
        CpuPercent = snapshot.CpuUsagePercent;
        CpuText = warmingUp ? "—" : Formatters.Percent(snapshot.CpuUsagePercent);
        MemoryPercent = snapshot.MemoryUsagePercent;
        MemoryText = Formatters.Percent(snapshot.MemoryUsagePercent);
        MemoryDetail = $"{Formatters.Bytes(snapshot.UsedMemoryBytes)} de {Formatters.Bytes(snapshot.TotalMemoryBytes)}";
        MemoryAvailableText = $"{Formatters.Bytes(Math.Min(snapshot.AvailableMemoryBytes, snapshot.TotalMemoryBytes))} disponíveis";

        if (!warmingUp && double.IsFinite(snapshot.CpuUsagePercent) && snapshot.TotalMemoryBytes > 0)
        {
            _history.Enqueue((Math.Clamp(snapshot.CpuUsagePercent, 0, 100), snapshot.MemoryUsagePercent));
            if (_history.Count > HistoryCapacity) _history.Dequeue();
            CpuHistoryPoints = CreateHistoryPoints(_history.Select(sample => sample.Cpu));
            MemoryHistoryPoints = CreateHistoryPoints(_history.Select(sample => sample.Memory));
        }

        Processes.Clear();
        foreach (var process in snapshot.Processes)
        {
            Processes.Add(new ProcessRow(process.Name, process.Id,
                Formatters.Percent(process.CpuPercent), Formatters.Bytes((ulong)Math.Max(0, process.WorkingSetBytes))));
        }

        Recommendations.Clear();
        if (warmingUp)
        {
            Recommendations.Add(new RecommendationRow("Preparando a leitura de CPU",
                "O consumo de CPU é calculado entre duas leituras. Aguarde a próxima atualização.", "#929EAE", "INICIANDO"));
        }
        else
        {
            foreach (var item in DiagnosticAnalyzer.Analyze(snapshot))
            {
                Recommendations.Add(new RecommendationRow(item.Title, item.Description,
                    item.Severity == RecommendationSeverity.Attention ? "#EAB978" : "#A5C9D2",
                    item.Severity == RecommendationSeverity.Attention ? "ATENÇÃO" : "OBSERVAÇÃO"));
            }
        }

        Status = warmingUp ? "Coletando a primeira amostra. Atualização automática a cada 2 segundos."
            : $"Atualizado às {snapshot.CapturedAt.ToLocalTime():HH:mm:ss} · Atualização automática a cada 2 segundos.";
        StatusColor = "#929EAE";
        MonitoringLabel = warmingUp ? "COLETANDO AMOSTRA" : "EM TEMPO REAL";
        MonitoringColor = warmingUp ? "#929EAE" : "#75DCEB";
        LastUpdatedText = warmingUp ? "Coletando amostra" : snapshot.CapturedAt.ToLocalTime().ToString("HH:mm:ss");
        NotifyAll();
    }

    public void SetError(string message)
    {
        Status = $"Não foi possível atualizar o diagnóstico: {message} Os dados anteriores podem estar desatualizados. Tente atualizar novamente.";
        StatusColor = "#EAB978";
        MonitoringLabel = "DADOS DESATUALIZADOS";
        MonitoringColor = "#EAB978";
        NotifyAll();
    }

    private static PointCollection CreateHistoryPoints(IEnumerable<double> samples)
    {
        var values = samples.ToArray();
        var points = new PointCollection();
        if (values.Length > 1)
        {
            for (var index = 0; index < values.Length; index++)
            {
                points.Add(new Point(index * 300d / (HistoryCapacity - 1),
                    46d - Math.Clamp(values[index], 0, 100) * 0.46d));
            }
        }

        points.Freeze();
        return points;
    }

    private void NotifyAll() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}

public sealed record ProcessRow(string Name, int Id, string Cpu, string Memory);
public sealed record RecommendationRow(string Title, string Description, string Color, string SeverityLabel = "OBSERVAÇÃO");
