using System.Collections.ObjectModel;
using System.ComponentModel;
using Optimizer.Core;

namespace Optimizer.App;

public sealed class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string ProcessorName { get; private set; } = "Identificando…";
    public string GraphicsCardName { get; private set; } = "Identificando…";
    public string OperatingSystem { get; private set; } = "Identificando…";
    public double CpuPercent { get; private set; }
    public string CpuText { get; private set; } = "—";
    public double MemoryPercent { get; private set; }
    public string MemoryText { get; private set; } = "—";
    public string MemoryDetail { get; private set; } = "Aguardando a primeira leitura.";
    public string Status { get; private set; } = "Iniciando diagnóstico…";
    public string StatusColor { get; private set; } = "#ACB9CC";
    public bool CanRefresh { get; private set; }
    public ObservableCollection<ProcessRow> Processes { get; } = [];
    public ObservableCollection<RecommendationRow> Recommendations { get; } = [];

    public void SetHardware(HardwareInfo hardware)
    {
        ProcessorName = hardware.ProcessorName;
        GraphicsCardName = hardware.GraphicsCardName;
        OperatingSystem = hardware.OperatingSystem;
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
                "O consumo de CPU é calculado entre duas leituras. Aguarde a próxima atualização.", "#ACB9CC"));
        }
        else
        {
            foreach (var item in DiagnosticAnalyzer.Analyze(snapshot))
            {
                Recommendations.Add(new RecommendationRow(item.Title, item.Description,
                    item.Severity == RecommendationSeverity.Attention ? "#FFD082" : "#63E2B7"));
            }
        }

        Status = warmingUp ? "Coletando a primeira amostra. Atualização automática a cada 2 segundos."
            : $"Atualizado às {snapshot.CapturedAt.ToLocalTime():HH:mm:ss} · Atualização automática a cada 2 segundos.";
        StatusColor = "#ACB9CC";
        NotifyAll();
    }

    public void SetError(string message)
    {
        Status = $"Não foi possível atualizar o diagnóstico: {message} Os dados anteriores podem estar desatualizados. Tente atualizar novamente.";
        StatusColor = "#FFD082";
        NotifyAll();
    }

    private void NotifyAll() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
}

public sealed record ProcessRow(string Name, int Id, string Cpu, string Memory);
public sealed record RecommendationRow(string Title, string Description, string Color);
