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
                "O uso da CPU chegou a 85% ou mais nesta amostra. Observe os processos ao reproduzir a lentidão para investigar a carga; isso, sozinho, não comprova um gargalo.",
                RecommendationSeverity.Attention));
        }

        if (snapshot.TotalMemoryBytes > 0 && snapshot.MemoryUsagePercent >= 85)
        {
            recommendations.Add(new Recommendation(
                "Memória com uso elevado",
                "85% ou mais da memória física está em uso. Revise os aplicativos abertos e compare o tempo de resposta do PC antes e depois de uma mudança.",
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
                $"{leadingProcess.Name} (PID {leadingProcess.Id}) usou {Formatters.Percent(leadingProcess.CpuPercent)} da capacidade total de CPU no intervalo. Verifique se é um aplicativo que você está usando ou um componente necessário antes de tomar qualquer ação.",
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
                "CPU e memória estão abaixo de 85% de uso. Observe o painel enquanto executa suas tarefas: uma amostra isolada não descarta travamentos nem identifica todos os gargalos.",
                RecommendationSeverity.Information));
        }

        recommendations.Add(new Recommendation(
            "Compare na mesma tarefa",
            "Para comparar mudanças, repita a mesma tarefa nas mesmas condições e observe o tempo de resposta do PC. Uma redução no consumo de CPU ou memória, por si só, não comprova melhoria de desempenho.",
            RecommendationSeverity.Information));

        return recommendations.AsReadOnly();
    }
}
