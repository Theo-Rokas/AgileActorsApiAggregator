using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AgileActorsApiAggregator.Core.Interfaces;
using AgileActorsApiAggregator.Core.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AgileActorsApiAggregator.IntegrationTests;

/// <summary>
/// Integration tests for the /api/aggregate and /api/stats endpoints.
/// Uses <see cref="WebApplicationFactory{TEntryPoint}"/> to spin up the full ASP.NET Core pipeline
/// with mocked external API clients, so no real HTTP calls are made.
/// </summary>
public class AggregateEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    private record TokenResponse(string Token);

    public AggregateEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithMocks(
        WeatherData? weather = null,
        IReadOnlyList<NewsArticle>? news = null,
        IReadOnlyList<GitHubRepo>? repos = null)
    {
        var weatherMock = new Mock<IWeatherClient>();
        weatherMock.Setup(w => w.GetWeatherAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(weather);

        var newsMock = new Mock<INewsClient>();
        newsMock.Setup(n => n.GetNewsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(news ?? []);

        var githubMock = new Mock<IGitHubClient>();
        githubMock.Setup(g => g.GetReposAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(repos ?? []);

        return _factory.WithWebHostBuilder(b =>
        {
            b.ConfigureServices(services =>
            {
                services.AddSingleton(weatherMock.Object);
                services.AddSingleton(newsMock.Object);
                services.AddSingleton(githubMock.Object);
            });
        }).CreateClient();
    }

    private async Task<string> GetTokenAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = "demo", password = "demo" });
        var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return result!.Token;
    }

    [Fact]
    public async Task PostToken_WithWrongCredentials_Returns401()
    {
        var client = CreateClientWithMocks();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = "wrong", password = "wrong" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostToken_WithEmptyCredentials_Returns401()
    {
        var client = CreateClientWithMocks();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username = "", password = "" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public static TheoryData<WeatherData?, List<NewsArticle>, List<GitHubRepo>> AggregateTestCases => new()
    {
        // correct input
        {
            new WeatherData("Athens", 25, "sunny", 60, 3.5),
            [new("Title", "Source", null, DateTime.UtcNow)],
            [new("repo", "user/repo", null, 100, "C#", DateTime.UtcNow)]
        },
        // empty input
        { null, [], [] }
    };

    [Theory]
    [MemberData(nameof(AggregateTestCases))]
    public async Task GetAggregate_WithToken_ReturnsExpectedData(
        WeatherData? weather,
        List<NewsArticle> news,
        List<GitHubRepo> repos)
    {
        var client = CreateClientWithMocks(weather, news, repos);
        var token = await GetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        for (var i = 0; i < 3; i++)
        {
            var response = await client.GetAsync("/api/aggregate?city=Athens&keyword=dotnet");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var data = await response.Content.ReadFromJsonAsync<AggregatedData>();
            data.Should().NotBeNull();
            if (weather != null) data!.Weather.Should().NotBeNull();
            else data!.Weather.Should().BeNull();
            data.News.Should().HaveCount(news.Count);
            data.Repos.Should().HaveCount(repos.Count);
        }
    }

    [Fact]
    public async Task GetAggregate_WithoutToken_Returns401()
    {
        var client = CreateClientWithMocks();
        var response = await client.GetAsync("/api/aggregate");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStats_WithToken_Returns200()
    {
        var client = CreateClientWithMocks();
        var token = await GetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        for (var i = 0; i < 3; i++)
        {
            var response = await client.GetAsync("/api/stats");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task GetStats_WithoutToken_Returns401()
    {
        var client = CreateClientWithMocks();
        var response = await client.GetAsync("/api/stats");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
