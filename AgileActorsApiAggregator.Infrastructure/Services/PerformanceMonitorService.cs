using AgileActorsApiAggregator.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgileActorsApiAggregator.Infrastructure.Services;

/// <summary>
/// Background service that runs every 5 minutes and logs a warning when any external API's
/// recent average response time is more than 50% above its all-time average — indicating
/// a potential performance degradation.
/// </summary>
public sealed class PerformanceMonitorService(
    IStatsStore stats,
    ILogger<PerformanceMonitorService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Interval, stoppingToken);
            CheckForPerformanceAnomalies();
        }
    }

    /// <summary>
    /// Compares each API's recent response times against its all-time average.
    /// Logs a warning if the recent average exceeds the all-time average by more than 50%.
    /// </summary>
    private void CheckForPerformanceAnomalies()
    {
        foreach (var apiStats in stats.GetAll())
        {
            if (apiStats.TotalRequests < 2) continue;

            var allTimes = stats.GetRecentTimes(apiStats.ApiName, Interval);
            if (allTimes.Length < 2) continue;

            var overallAvg = allTimes.Average();

            // Approximate "recent" as the last 20% of recorded entries
            var recentCount = Math.Max(1, allTimes.Length / 5);
            var recentAvg = allTimes.TakeLast(recentCount).Average();

            if (recentAvg > overallAvg * 1.5)
            {
                logger.LogWarning(
                    "Performance anomaly detected for {Api}: recent avg {Recent:F1}ms is >50% above overall avg {Overall:F1}ms",
                    apiStats.ApiName, recentAvg, overallAvg);
            }
        }
    }
}
