namespace Optimizer.Core;

public enum RecommendationSeverity
{
    Information,
    Attention
}

public sealed record Recommendation(
    string Title,
    string Description,
    RecommendationSeverity Severity);
