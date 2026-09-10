namespace AgileActorsApiAggregator.Core.Models;

/// <summary>Query parameters accepted by the aggregation endpoint for filtering and sorting results.</summary>
public class AggregationQuery
{
    /// <summary>City name passed to OpenWeatherMap (e.g. "Athens").</summary>
    public string City { get; set; } = "Athens";

    /// <summary>Search keyword used for both NewsAPI and GitHub queries (e.g. "technology").</summary>
    public string Keyword { get; set; } = "technology";

    /// <summary>Field to sort by: "date" (default), "relevance" (news), or "stars" (GitHub).</summary>
    public string SortBy { get; set; } = "date";

    /// <summary>Sort direction: "desc" (default) or "asc".</summary>
    public string SortOrder { get; set; } = "desc";

    /// <summary>Maximum number of items returned per data source.</summary>
    public int PageSize { get; set; } = 5;
}
