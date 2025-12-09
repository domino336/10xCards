namespace _10xCards.Application.DTOs.Admin;

public sealed record AdminMetricsDto(
    int TotalCards,
    int TotalAiCards,
    int TotalManualCards,
    double AiAcceptanceRate,
    PercentileData AcceptancePercentiles,
    int ActiveUsers7d,
    int ActiveUsers30d,
    int GenerationErrorsLast7d);

public sealed record PercentileData(
    double P50,
    double P75,
    double P90);
