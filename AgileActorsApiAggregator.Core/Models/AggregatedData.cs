namespace AgileActorsApiAggregator.Core.Models;

/// <summary>Combined response returned by the aggregation endpoint.</summary>
public record AggregatedData(
    WeatherData? Weather,
    IReadOnlyList<NewsArticle> News,
    IReadOnlyList<GitHubRepo> Repos
);

/// <summary>Current weather conditions for a city from OpenWeatherMap.</summary>
public record WeatherData(
    string City,
    double TemperatureC,
    string Description,
    double Humidity,
    double WindSpeed
);

/// <summary>A single news article returned by NewsAPI.</summary>
public record NewsArticle(
    string Title,
    string Source,
    string? Url,
    DateTime PublishedAt
);

/// <summary>A GitHub repository returned by the GitHub Search API.</summary>
public record GitHubRepo(
    string Name,
    string FullName,
    string? Description,
    int Stars,
    string Language,
    DateTime UpdatedAt
);
