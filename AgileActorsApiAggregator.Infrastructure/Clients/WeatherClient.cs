using System.Diagnostics;
using AgileActorsApiAggregator.Core.Interfaces;
using AgileActorsApiAggregator.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgileActorsApiAggregator.Infrastructure.Clients;

/// <summary>
/// Fetches current weather data via the OpenWeatherMap API.
/// Results are cached for 5 minutes to avoid redundant API calls.
/// Returns null if the request fails, allowing aggregation to continue without weather data.
/// </summary>
public sealed class WeatherClient(
    IWeatherApi api,
    IMemoryCache cache,
    IStatsStore stats,
    IConfiguration config,
    ILogger<WeatherClient> logger) : IWeatherClient
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const string ApiName = "OpenWeatherMap";

    public async Task<WeatherData?> GetWeatherAsync(string city, CancellationToken ct = default)
    {
        var cacheKey = $"weather:{city}";
        if (cache.TryGetValue(cacheKey, out WeatherData? cached))
        {
            // Cache hit — record a zero-latency entry so the stats store stays accurate
            stats.Record(ApiName, 0);
            return cached;
        }

        var apiKey = config["ExternalApis:OpenWeatherMap:ApiKey"]!;

        var sw = Stopwatch.StartNew();
        try
        {
            var r = await api.GetWeatherAsync(city, apiKey, ct);
            sw.Stop();
            stats.Record(ApiName, sw.Elapsed.TotalMilliseconds);

            var result = new WeatherData(
                r.Name, r.Main.Temp,
                r.Weather[0].Description,
                r.Main.Humidity, r.Wind.Speed);

            cache.Set(cacheKey, result, CacheDuration);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            stats.Record(ApiName, sw.Elapsed.TotalMilliseconds);
            logger.LogError(ex, "Failed to fetch weather for {City}", city);
            return null; // Graceful fallback — aggregation continues without weather data
        }
    }
}
