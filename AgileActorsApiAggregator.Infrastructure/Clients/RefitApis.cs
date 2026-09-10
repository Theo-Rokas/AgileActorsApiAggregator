using Refit;

namespace AgileActorsApiAggregator.Infrastructure.Clients;

// Internal DTOs — these map directly to the raw JSON shapes returned by each external API.
// They are not exposed outside the Infrastructure layer; domain models live in Core.Models.

/// <summary>Top-level response from the GitHub repository search endpoint.</summary>
public record GitHubSearchResponse(List<GitHubRepoDto> Items);

/// <summary>A single repository entry in the GitHub search response.</summary>
public record GitHubRepoDto(
    string Name,
    string Full_Name,
    string? Description,
    int Stargazers_Count,
    string? Language,
    DateTime Updated_At);

/// <summary>Top-level response from the NewsAPI /v2/everything endpoint.</summary>
public record NewsResponse(List<NewsArticleDto> Articles);

/// <summary>A single article entry in the NewsAPI response.</summary>
public record NewsArticleDto(string Title, NewsSourceDto Source, string? Url, DateTime PublishedAt);

/// <summary>The source object nested inside a NewsAPI article.</summary>
public record NewsSourceDto(string Name);

/// <summary>Top-level response from the OpenWeatherMap current-weather endpoint.</summary>
public record WeatherResponse(string Name, WeatherMain Main, List<WeatherDesc> Weather, WeatherWind Wind);

/// <summary>Temperature and humidity fields from the OpenWeatherMap response.</summary>
public record WeatherMain(double Temp, double Humidity);

/// <summary>Weather condition description from the OpenWeatherMap response.</summary>
public record WeatherDesc(string Description);

/// <summary>Wind speed from the OpenWeatherMap response.</summary>
public record WeatherWind(double Speed);

/// <summary>Refit interface for the GitHub Search API.</summary>
[Headers("User-Agent: AgileActorsApiAggregator")]
public interface IGitHubApi
{
    [Get("/search/repositories?sort=stars&order=desc&per_page=10")]
    Task<GitHubSearchResponse> SearchReposAsync([AliasAs("q")] string query, CancellationToken ct = default);
}

/// <summary>Refit interface for the NewsAPI /v2/everything endpoint.</summary>
[Headers("User-Agent: AgileActorsApiAggregator")]
public interface INewsApi
{
    [Get("/v2/everything?sortBy=publishedAt&pageSize=10&language=en")]
    Task<NewsResponse> GetEverythingAsync([AliasAs("q")] string query, [AliasAs("apiKey")] string apiKey, CancellationToken ct = default);
}

/// <summary>Refit interface for the OpenWeatherMap current-weather endpoint.</summary>
public interface IWeatherApi
{
    [Get("/data/2.5/weather?units=metric")]
    Task<WeatherResponse> GetWeatherAsync([AliasAs("q")] string city, [AliasAs("appid")] string apiKey, CancellationToken ct = default);
}
