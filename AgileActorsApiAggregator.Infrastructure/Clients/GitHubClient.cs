using System.Diagnostics;
using AgileActorsApiAggregator.Core.Interfaces;
using AgileActorsApiAggregator.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AgileActorsApiAggregator.Infrastructure.Clients;

/// <summary>
/// Fetches GitHub repositories via the GitHub Search API.
/// Results are cached for 5 minutes to avoid redundant API calls.
/// Falls back to an empty list if the request fails.
/// </summary>
public sealed class GitHubClient(
    IGitHubApi api,
    IMemoryCache cache,
    IStatsStore stats,
    ILogger<GitHubClient> logger) : IGitHubClient
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const string ApiName = "GitHub";

    public async Task<IReadOnlyList<GitHubRepo>> GetReposAsync(string keyword, CancellationToken ct = default)
    {
        var cacheKey = $"github:{keyword}";
        if (cache.TryGetValue(cacheKey, out IReadOnlyList<GitHubRepo>? cached))
        {
            // Cache hit — record a zero-latency entry so the stats store stays accurate
            stats.Record(ApiName, 0);
            return cached!;
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var result = await api.SearchReposAsync(keyword, ct);
            sw.Stop();
            stats.Record(ApiName, sw.Elapsed.TotalMilliseconds);

            var repos = result.Items.Select(r => new GitHubRepo(
                r.Name, r.Full_Name, r.Description, r.Stargazers_Count,
                r.Language ?? "", r.Updated_At)).ToList();

            cache.Set(cacheKey, (IReadOnlyList<GitHubRepo>)repos, CacheDuration);
            return repos;
        }
        catch (Exception ex)
        {
            sw.Stop();
            stats.Record(ApiName, sw.Elapsed.TotalMilliseconds);
            logger.LogError(ex, "Failed to fetch GitHub repos for {Keyword}", keyword);
            return []; // Graceful fallback — aggregation continues without GitHub data
        }
    }
}
