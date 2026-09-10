using AgileActorsApiAggregator.Core.Interfaces;
using AgileActorsApiAggregator.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgileActorsApiAggregator.Api.Controllers;

/// <summary>Exposes the unified aggregation endpoint that combines weather, news, and GitHub data.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AggregateController(IAggregationService aggregation) : ControllerBase
{
    /// <summary>
    /// Returns aggregated data from OpenWeatherMap, NewsAPI, and GitHub.
    /// </summary>
    /// <param name="query">Filter and sort parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    public async Task<ActionResult<AggregatedData>> Get([FromQuery] AggregationQuery query, CancellationToken ct)
    {
        var result = await aggregation.AggregateAsync(query, ct);
        return Ok(result);
    }
}
