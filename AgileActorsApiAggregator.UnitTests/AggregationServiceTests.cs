using AgileActorsApiAggregator.Core.Interfaces;
using AgileActorsApiAggregator.Core.Models;
using AgileActorsApiAggregator.Infrastructure.Services;
using FluentAssertions;
using Moq;

namespace AgileActorsApiAggregator.Tests;

/// <summary>
/// Unit tests for <see cref="AggregationService"/>.
/// Verifies parallel data fetching, per-source fallback behaviour, sorting, and paging.
/// </summary>
public class AggregationServiceTests
{
    private readonly Mock<IWeatherClient> _weather = new();
    private readonly Mock<INewsClient> _news = new();
    private readonly Mock<IGitHubClient> _github = new();

    private AggregationService CreateService() => new(_weather.Object, _news.Object, _github.Object);

    private static readonly WeatherData SampleWeather = new("Athens", 25, "sunny", 60, 3.5);

    private static readonly List<NewsArticle> SampleNews =
    [
        new("B Article", "Source1", null, new DateTime(2024, 1, 2)),
        new("A Article", "Source2", null, new DateTime(2024, 1, 1)),
        new("C Article", "Source3", null, new DateTime(2024, 1, 3))
    ];

    private static readonly List<GitHubRepo> SampleRepos =
    [
        new("repo-b", "user/repo-b", null, 200, "C#", new DateTime(2024, 1, 2)),
        new("repo-a", "user/repo-a", null, 100, "Go", new DateTime(2024, 1, 1)),
        new("repo-c", "user/repo-c", null, 300, "Python", new DateTime(2024, 1, 3))
    ];

    [Fact]
    public async Task AggregateAsync_ReturnsAllSources()
    {
        _weather.Setup(w => w.GetWeatherAsync("Athens", default)).ReturnsAsync(SampleWeather);
        _news.Setup(n => n.GetNewsAsync("tech", default)).ReturnsAsync(SampleNews);
        _github.Setup(g => g.GetReposAsync("tech", default)).ReturnsAsync(SampleRepos);

        var service = CreateService();
        for (var i = 0; i < 3; i++)
        {
            var result = await service.AggregateAsync(new AggregationQuery { City = "Athens", Keyword = "tech" });

            result.Weather.Should().Be(SampleWeather);
            result.News.Should().HaveCount(3);
            result.Repos.Should().HaveCount(3);
        }
    }

    [Fact]
    public async Task AggregateAsync_WeatherNull_StillReturnsNewsAndRepos()
    {
        _weather.Setup(w => w.GetWeatherAsync(It.IsAny<string>(), default)).ReturnsAsync((WeatherData?)null);
        _news.Setup(n => n.GetNewsAsync(It.IsAny<string>(), default)).ReturnsAsync(SampleNews);
        _github.Setup(g => g.GetReposAsync(It.IsAny<string>(), default)).ReturnsAsync(SampleRepos);

        var result = await CreateService().AggregateAsync(new AggregationQuery());

        result.Weather.Should().BeNull();
        result.News.Should().NotBeEmpty();
        result.Repos.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AggregateAsync_NewsEmpty_StillReturnsWeatherAndRepos()
    {
        _weather.Setup(w => w.GetWeatherAsync(It.IsAny<string>(), default)).ReturnsAsync(SampleWeather);
        _news.Setup(n => n.GetNewsAsync(It.IsAny<string>(), default)).ReturnsAsync([]);
        _github.Setup(g => g.GetReposAsync(It.IsAny<string>(), default)).ReturnsAsync(SampleRepos);

        var result = await CreateService().AggregateAsync(new AggregationQuery());

        result.Weather.Should().NotBeNull();
        result.News.Should().BeEmpty();
        result.Repos.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AggregateAsync_ReposEmpty_StillReturnsNewsAndWeather()
    {
        _weather.Setup(w => w.GetWeatherAsync(It.IsAny<string>(), default)).ReturnsAsync(SampleWeather);
        _news.Setup(n => n.GetNewsAsync(It.IsAny<string>(), default)).ReturnsAsync(SampleNews);
        _github.Setup(g => g.GetReposAsync(It.IsAny<string>(), default)).ReturnsAsync([]);

        var result = await CreateService().AggregateAsync(new AggregationQuery());

        result.Weather.Should().NotBeNull();
        result.News.Should().NotBeEmpty();
        result.Repos.Should().BeEmpty();
    }

    [Fact]
    public async Task AggregateAsync_SortByDate_Desc_NewsOrderedNewestFirst()
    {
        _weather.Setup(w => w.GetWeatherAsync(It.IsAny<string>(), default)).ReturnsAsync(SampleWeather);
        _news.Setup(n => n.GetNewsAsync(It.IsAny<string>(), default)).ReturnsAsync(SampleNews);
        _github.Setup(g => g.GetReposAsync(It.IsAny<string>(), default)).ReturnsAsync(SampleRepos);

        var query = new AggregationQuery { SortBy = "date", SortOrder = "desc" };
        var result = await CreateService().AggregateAsync(query);

        result.News[0].Title.Should().Be("C Article");
        result.News[1].Title.Should().Be("B Article");
        result.News[2].Title.Should().Be("A Article");
    }

    [Fact]
    public async Task AggregateAsync_SortByStars_Desc_ReposOrderedByStarsDescending()
    {
        _weather.Setup(w => w.GetWeatherAsync(It.IsAny<string>(), default)).ReturnsAsync(SampleWeather);
        _news.Setup(n => n.GetNewsAsync(It.IsAny<string>(), default)).ReturnsAsync(SampleNews);
        _github.Setup(g => g.GetReposAsync(It.IsAny<string>(), default)).ReturnsAsync(SampleRepos);

        var query = new AggregationQuery { SortBy = "stars", SortOrder = "desc" };
        var result = await CreateService().AggregateAsync(query);

        result.Repos[0].Stars.Should().Be(300);
        result.Repos[1].Stars.Should().Be(200);
        result.Repos[2].Stars.Should().Be(100);
    }

    [Fact]
    public async Task AggregateAsync_PageSize_LimitsResults()
    {
        _weather.Setup(w => w.GetWeatherAsync(It.IsAny<string>(), default)).ReturnsAsync(SampleWeather);
        _news.Setup(n => n.GetNewsAsync(It.IsAny<string>(), default)).ReturnsAsync(SampleNews);
        _github.Setup(g => g.GetReposAsync(It.IsAny<string>(), default)).ReturnsAsync(SampleRepos);

        var query = new AggregationQuery { PageSize = 2 };
        var result = await CreateService().AggregateAsync(query);

        result.News.Should().HaveCount(2);
        result.Repos.Should().HaveCount(2);
    }

}
