using AgileActorsApiAggregator.Core.Interfaces;
using AgileActorsApiAggregator.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgileActorsApiAggregator.Api.Controllers;

/// <summary>Exposes per-API request statistics grouped by performance bucket.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatsController(IStatsStore stats) : ControllerBase
{
    /// <summary>
    /// Returns per-API request statistics grouped into performance buckets.
    /// </summary>
    [HttpGet]
    public ActionResult<IReadOnlyList<ApiStats>> Get() => Ok(stats.GetAll());
}
