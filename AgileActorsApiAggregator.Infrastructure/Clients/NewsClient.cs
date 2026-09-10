using System.Diagnostics;
using AgileActorsApiAggregator.Core.Interfaces;
using AgileActorsApiAggregator.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgileActorsApiAggregator.Infrastructure.Clients;

/// <summary>
/// Fetches news articles via the NewsAPI /v2/everything endpoint.
/// Results are cached for 5 minutes to avoid redundant API calls.
/// Falls back to an empty list if the request fails.
/// </summary>
public sealed class NewsClient(
    INewsApi api,
    IMemoryCache cache,
    IStatsStore stats,
    IConfiguration config,
    ILogger<NewsClient> logger) : INewsClient
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const string ApiName = "NewsAPI";

    public async Task<IReadOnlyList<NewsArticle>> GetNewsAsync(string keyword, CancellationToken ct = default)
    {
        var cacheKey = $"news:{keyword}";
        if (cache.TryGetValue(cacheKey, out IReadOnlyList<NewsArticle>? cached))
        {
            // Cache hit — record a zero-latency entry so the stats store stays accurate
            stats.Record(ApiName, 0);
            return cached!;
        }

        var apiKey = config["ExternalApis:NewsApi:ApiKey"]!;

        var sw = Stopwatch.StartNew();
        try
        {
            var result = await api.GetEverythingAsync(keyword, apiKey, ct);
            sw.Stop();
            stats.Record(ApiName, sw.Elapsed.TotalMilliseconds);

            var articles = result.Articles.Select(a => new NewsArticle(
                a.Title, a.Source.Name, a.Url, a.PublishedAt)).ToList();

            cache.Set(cacheKey, (IReadOnlyList<NewsArticle>)articles, CacheDuration);
            return articles;
        }
        catch (Exception ex)
        {
            sw.Stop();
            stats.Record(ApiName, sw.Elapsed.TotalMilliseconds);
            logger.LogError(ex, "Failed to fetch news for {Keyword}", keyword);
            return []; // Graceful fallback — aggregation continues without news data
        }
    }
}
