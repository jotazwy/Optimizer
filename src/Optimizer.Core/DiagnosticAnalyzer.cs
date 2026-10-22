namespace Optimizer.Core;

public static class DiagnosticAnalyzer
{
    public static IReadOnlyList<Recommendation> Analyze(SystemSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var recommendations = new List<Recommendation>();

        if (double.IsFinite(snapshot.CpuUsagePercent) && snapshot.CpuUsagePercent >= 85)
        {
            recommendations.Add(new Recommendation(
                "CPU com uso elevado",
                "O uso da CPU chegou a 85% ou mais nesta amostra. Compare os processos durante o jogo para investigar a carga; isso, sozinho, não comprova um gargalo.",
                RecommendationSeverity.Attention));
        }

        if (snapshot.TotalMemoryBytes > 0 && snapshot.MemoryUsagePercent >= 85)
        {
            recommendations.Add(new Recommendation(
                "Memória com uso elevado",
                "85% ou mais da memória física está em uso. Revise os aplicativos abertos e compare a estabilidade do jogo antes e depois de uma mudança.",
                RecommendationSeverity.Attention));
        }

        var leadingProcess = snapshot.Processes
            .Where(process => process.CpuPercent is double cpu && double.IsFinite(cpu) && cpu >= 10)
            .OrderByDescending(process => process.CpuPercent)
            .ThenBy(process => process.Id)
            .FirstOrDefault();

        if (leadingProcess is not null)
        {
            recommendations.Add(new Recommendation(
                "Processo para investigar",
                $"{leadingProcess.Name} (PID {leadingProcess.Id}) usou {Formatters.Percent(leadingProcess.CpuPercent)} da capacidade total de CPU no intervalo. Verifique se é o próprio jogo ou um aplicativo necessário antes de tomar qualquer ação.",
                RecommendationSeverity.Information));
        }

        if (snapshot.TotalMemoryBytes == 0 || !double.IsFinite(snapshot.CpuUsagePercent))
        {
            recommendations.Add(new Recommendation(
                "Diagnóstico parcial",
                "Algumas métricas não estão disponíveis nesta amostra. Aguarde uma nova coleta antes de avaliar o uso de CPU e memória.",
                RecommendationSeverity.Information));
        }
        else if (recommendations.Count == 0)
        {
            recommendations.Add(new Recommendation(
                "Sem pressão alta nesta amostra",
                "CPU e memória estão abaixo de 85% de uso. Observe o painel durante o jogo: uma amostra isolada não descarta travamentos nem identifica todos os gargalos.",
                RecommendationSeverity.Information));
        }

        recommendations.Add(new Recommendation(
            "Compare durante o jogo",
            "Este painel mostra recursos do sistema; ele não mede FPS nem input lag. Para comparar mudanças, use o mesmo jogo, cenário e configurações, com uma ferramenta de captura de tempos de quadro.",
            RecommendationSeverity.Information));

        return recommendations.AsReadOnly();
    }
}
