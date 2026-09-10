namespace AgileActorsApiAggregator.Core.Models;

/// <summary>Aggregated request statistics for a single external API.</summary>
public record ApiStats(
    string ApiName,
    int TotalRequests,
    double AverageResponseMs,
    PerformanceBuckets Buckets
);

/// <summary>Request counts grouped by response-time performance tier.</summary>
public record PerformanceBuckets(
    int Fast,    // < 100 ms
    int Average, // 100–200 ms
    int Slow     // > 200 ms
);
