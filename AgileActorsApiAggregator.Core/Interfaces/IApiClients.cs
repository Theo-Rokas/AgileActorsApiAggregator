using AgileActorsApiAggregator.Core.Models;

namespace AgileActorsApiAggregator.Core.Interfaces;

/// <summary>Fetches current weather data from OpenWeatherMap.</summary>
public interface IWeatherClient
{
    /// <summary>Returns weather for the given city, or null if the request fails.</summary>
    Task<WeatherData?> GetWeatherAsync(string city, CancellationToken ct = default);
}

/// <summary>Fetches news articles from NewsAPI.</summary>
public interface INewsClient
{
    /// <summary>Returns articles matching the keyword. Returns an empty list on failure.</summary>
    Task<IReadOnlyList<NewsArticle>> GetNewsAsync(string keyword, CancellationToken ct = default);
}

/// <summary>Fetches GitHub repository data from the GitHub Search API.</summary>
public interface IGitHubClient
{
    /// <summary>Returns repositories matching the keyword. Returns an empty list on failure.</summary>
    Task<IReadOnlyList<GitHubRepo>> GetReposAsync(string keyword, CancellationToken ct = default);
}

/// <summary>Orchestrates parallel calls to all external API clients and returns combined results.</summary>
public interface IAggregationService
{
    /// <summary>Fetches weather, news, and GitHub repos in parallel, then applies sorting and paging.</summary>
    Task<AggregatedData> AggregateAsync(AggregationQuery query, CancellationToken ct = default);
}

/// <summary>Thread-safe store for recording and querying per-API request statistics.</summary>
public interface IStatsStore
{
    /// <summary>Records a completed request for the named API with its elapsed time in milliseconds.</summary>
    void Record(string apiName, double elapsedMs);

    /// <summary>Returns aggregated statistics (totals, averages, performance buckets) for every API.</summary>
    IReadOnlyList<ApiStats> GetAll();

    /// <summary>Returns the raw response times recorded for the named API within the given time window.</summary>
    double[] GetRecentTimes(string apiName, TimeSpan window);
}
