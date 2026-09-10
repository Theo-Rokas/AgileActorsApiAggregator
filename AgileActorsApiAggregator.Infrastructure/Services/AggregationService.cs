using AgileActorsApiAggregator.Core.Interfaces;
using AgileActorsApiAggregator.Core.Models;

namespace AgileActorsApiAggregator.Infrastructure.Services;

/// <summary>
/// Calls all three external API clients in parallel, then applies sorting and paging
/// to the combined results before returning them as a single <see cref="AggregatedData"/> response.
/// </summary>
public sealed class AggregationService(
    IWeatherClient weather,
    INewsClient news,
    IGitHubClient github) : IAggregationService
{
    public async Task<AggregatedData> AggregateAsync(AggregationQuery query, CancellationToken ct = default)
    {
        // Fire all three requests simultaneously to minimise total response time
        var weatherTask = weather.GetWeatherAsync(query.City, ct);
        var newsTask = news.GetNewsAsync(query.Keyword, ct);
        var reposTask = github.GetReposAsync(query.Keyword, ct);

        await Task.WhenAll(weatherTask, newsTask, reposTask);

        var newsItems = ApplySortingToNews(newsTask.Result, query);
        var repoItems = ApplySortingToRepos(reposTask.Result, query);

        return new AggregatedData(
            Weather: weatherTask.Result,
            News: newsItems.Take(query.PageSize).ToList(),
            Repos: repoItems.Take(query.PageSize).ToList()
        );
    }

    /// <summary>Sorts news articles by date or relevance (title) in the requested direction.</summary>
    private static IEnumerable<NewsArticle> ApplySortingToNews(IReadOnlyList<NewsArticle> items, AggregationQuery query)
    {
        IOrderedEnumerable<NewsArticle> ordered = query.SortBy switch
        {
            "relevance" => query.SortOrder == "asc"
                ? items.OrderBy(n => n.Title)
                : items.OrderByDescending(n => n.Title),
            _ => query.SortOrder == "asc"
                ? items.OrderBy(n => n.PublishedAt)
                : items.OrderByDescending(n => n.PublishedAt)
        };
        return ordered;
    }

    /// <summary>Sorts GitHub repositories by star count or last-updated date in the requested direction.</summary>
    private static IEnumerable<GitHubRepo> ApplySortingToRepos(IReadOnlyList<GitHubRepo> items, AggregationQuery query)
    {
        IOrderedEnumerable<GitHubRepo> ordered = query.SortBy switch
        {
            "stars" => query.SortOrder == "asc"
                ? items.OrderBy(r => r.Stars)
                : items.OrderByDescending(r => r.Stars),
            _ => query.SortOrder == "asc"
                ? items.OrderBy(r => r.UpdatedAt)
                : items.OrderByDescending(r => r.UpdatedAt)
        };
        return ordered;
    }
}
