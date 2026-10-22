using Optimizer.Core;
using Xunit;

namespace Optimizer.Core.Tests;

public sealed class DiagnosticAnalyzerTests
{
    [Theory]
    [InlineData(84.99, 151UL, false, false)]
    [InlineData(85, 151UL, true, false)]
    [InlineData(84.99, 150UL, false, true)]
    [InlineData(85, 150UL, true, true)]
    public void PressureRecommendationsStartAtEightyFivePercent(
        double cpu, ulong available, bool expectedCpuPressure, bool expectedMemoryPressure)
    {
        var recommendations = DiagnosticAnalyzer.Analyze(Snapshot(cpu, available));

        Assert.Equal(expectedCpuPressure, recommendations.Any(item => item.Title == "CPU com uso elevado"));
        Assert.Equal(expectedMemoryPressure, recommendations.Any(item => item.Title == "Memória com uso elevado"));
    }

    [Fact]
    public void LeadingProcessUsesHighestMeasuredCpuAndIgnoresMissingOrInvalidSamples()
    {
        ProcessSnapshot[] processes =
        [
            new(1, "sem amostra", null, 100),
            new(2, "menos CPU", 10, 100),
            new(3, "mais CPU", 35, 100),
            new(4, "métrica inválida", double.NaN, 100),
            new(5, "infinita", double.PositiveInfinity, 100)
        ];

        var recommendations = DiagnosticAnalyzer.Analyze(Snapshot(processes: processes));
        var processRecommendation = Assert.Single(recommendations, item => item.Title == "Processo para investigar");

        Assert.Contains("mais CPU (PID 3)", processRecommendation.Description);
        Assert.Contains("35,0%", processRecommendation.Description);
        Assert.Contains("próprio jogo", processRecommendation.Description);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(9.99, false)]
    [InlineData(10d, true)]
    public void ProcessRecommendationNeedsMeasuredCpuOfAtLeastTenPercent(double? processCpu, bool expected)
    {
        var recommendations = DiagnosticAnalyzer.Analyze(Snapshot(processes: [new(42, "Aplicativo", processCpu, 100)]));

        Assert.Equal(expected, recommendations.Any(item => item.Title == "Processo para investigar"));
    }

    [Fact]
    public void NormalSampleDoesNotClaimToMeasureFpsOrLatency()
    {
        var recommendations = DiagnosticAnalyzer.Analyze(Snapshot());

        Assert.Contains(recommendations, item => item.Title == "Sem pressão alta nesta amostra");
        Assert.Contains(recommendations, item => item.Description.Contains("não mede FPS nem input lag"));
        Assert.All(recommendations, item => Assert.Equal(RecommendationSeverity.Information, item.Severity));
    }

    [Fact]
    public void MissingMemoryAndCpuDoNotProduceFalseHealthyRecommendation()
    {
        var snapshot = new SystemSnapshot(DateTimeOffset.UtcNow, double.NaN, 0, 0, []);

        var recommendations = DiagnosticAnalyzer.Analyze(snapshot);

        Assert.Contains(recommendations, item => item.Title == "Diagnóstico parcial");
        Assert.DoesNotContain(recommendations, item => item.Title == "Sem pressão alta nesta amostra");
    }

    private static SystemSnapshot Snapshot(
        double cpu = 20,
        ulong available = 500,
        IReadOnlyList<ProcessSnapshot>? processes = null) =>
        new(DateTimeOffset.UtcNow, cpu, 1_000, available, processes ?? []);
}
