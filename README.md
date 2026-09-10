# AgileActors API Aggregator

An ASP.NET Core 8 service that fetches data from three external APIs in parallel, **OpenWeatherMap**, **NewsAPI**, and **GitHub**, and exposes the combined result through a single authenticated endpoint.

---

## Features

- Parallel data fetching from three external APIs
- Filtering and sorting of results by date, relevance, or star count
- In-memory response caching to reduce redundant API calls
- Per-API request statistics with performance bucket classification
- JWT bearer authentication on all data endpoints
- Background service that detects and logs performance anomalies
- Swagger UI with JWT support for interactive testing

---

## Project Structure

```
AgileActorsApiAggregator.Api              # ASP.NET Core web host, controllers, Program.cs
AgileActorsApiAggregator.Core             # Interfaces and domain models (no dependencies)
AgileActorsApiAggregator.Infrastructure   # API clients, aggregation service, stats store
AgileActorsApiAggregator.UnitTests        # xUnit unit tests (AggregationService, StatsStore)
AgileActorsApiAggregator.IntegrationTests # xUnit integration tests (full pipeline via WebApplicationFactory)
```

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- API keys for the three external services (see Configuration below)

---

## Configuration

API keys and JWT settings are read from `appsettings.json`. Replace the placeholder values before running:

```json
{
  "Jwt": {
    "Key": "<your-secret-key-min-32-chars>",
    "Issuer": "AgileActorsApiAggregator",
    "Audience": "AgileActorsApiAggregatorUsers"
  },
  "ExternalApis": {
    "OpenWeatherMap": { "ApiKey": "<your-openweathermap-api-key>" },
    "NewsApi": { "ApiKey": "<your-newsapi-api-key>" },
    "GitHub": { "Token": "<your-github-personal-access-token>" }
  }
}
```

| Setting                 | Where to get it                    |
| ----------------------- | ---------------------------------- |
| `OpenWeatherMap.ApiKey` | https://openweathermap.org/api     |
| `NewsApi.ApiKey`        | https://newsapi.org                |
| `GitHub.Token`          | https://github.com/settings/tokens |

---

## Running the API

From the solution root:

```bash
dotnet run --project AgileActorsApiAggregator.Api
```

Swagger UI is available at `https://localhost:<port>/swagger` when running in the Development environment.

---

## Authentication

All data endpoints require a JWT bearer token.

**1. Obtain a token**

```http
POST /api/auth/login
Content-Type: application/json

{ "username": "demo", "password": "demo" }
```

Response:

```json
{ "token": "<jwt-token>" }
```

**2. Use the token**

Pass the token in the `Authorization` header for subsequent requests:

```
Authorization: Bearer <jwt-token>
```

In Swagger UI click **Authorize** and paste the token (without the `Bearer ` prefix).

---

## Endpoints

### `GET /api/aggregate`

Returns weather, news, and GitHub repository data combined into a single response.

**Query parameters**

| Parameter   | Type   | Default      | Description                                                 |
| ----------- | ------ | ------------ | ----------------------------------------------------------- |
| `city`      | string | `Athens`     | City name for the weather lookup                            |
| `keyword`   | string | `technology` | Search term for news articles and GitHub repositories       |
| `sortBy`    | string | `date`       | Sort field: `date`, `relevance` (news), or `stars` (GitHub) |
| `sortOrder` | string | `desc`       | Sort direction: `asc` or `desc`                             |
| `pageSize`  | int    | `5`          | Maximum number of items returned per source                 |

**Example request**

```http
GET /api/aggregate?city=London&keyword=dotnet&sortBy=stars&sortOrder=desc&pageSize=3
Authorization: Bearer <token>
```

**Example response**

```json
{
  "weather": {
    "city": "London",
    "temperatureC": 14.2,
    "description": "overcast clouds",
    "humidity": 78,
    "windSpeed": 5.1
  },
  "news": [
    {
      "title": "Microsoft releases .NET 9",
      "source": "The Verge",
      "url": "https://theverge.com/...",
      "publishedAt": "2024-11-12T10:00:00Z"
    }
  ],
  "repos": [
    {
      "name": "awesome-dotnet",
      "fullName": "quozd/awesome-dotnet",
      "description": "A collection of awesome .NET libraries",
      "stars": 19200,
      "language": "C#",
      "updatedAt": "2024-11-01T08:30:00Z"
    }
  ]
}
```

---

### `GET /api/stats`

Returns request statistics for each external API, grouped by performance bucket.

**Example response**

```json
[
  {
    "apiName": "OpenWeatherMap",
    "totalRequests": 42,
    "averageResponseMs": 143.7,
    "buckets": {
      "fast": 10,
      "average": 25,
      "slow": 7
    }
  }
]
```

**Performance buckets**

| Bucket    | Response time |
| --------- | ------------- |
| `fast`    | < 100 ms      |
| `average` | 100 – 200 ms  |
| `slow`    | > 200 ms      |

---

## Running the Tests

Run these from the solution root. If the server is running, use a **separate terminal** for tests — otherwise the build will fail because the server locks the output binaries.

```bash
# Unit tests
dotnet test AgileActorsApiAggregator.UnitTests

# Integration tests
dotnet test AgileActorsApiAggregator.IntegrationTests

# All tests
dotnet test
```

> The integration tests use `WebApplicationFactory` and mock all external API clients, so no real API keys or running server are required.

---

## Key Design Decisions

- **Refit** is used for all external HTTP clients, keeping HTTP plumbing declarative and minimal.
- **IMemoryCache** caches each API response for 5 minutes, keyed by the query parameters, to avoid redundant calls on repeated requests.
- **ConcurrentDictionary / ConcurrentBag** in `InMemoryStatsStore` ensure thread-safe writes from parallel API calls without explicit locking.
- **`Task.WhenAll`** in `AggregationService` fires all three API calls simultaneously, so total latency is bounded by the slowest single call rather than their sum.
- Each API client catches all exceptions and returns a safe empty/null fallback, so a single failing API never breaks the aggregated response.
- The **`PerformanceMonitorService`** background service logs a warning when any API's recent average response time exceeds its all-time average by more than 50%.
