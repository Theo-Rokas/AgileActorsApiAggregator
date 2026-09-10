using System.Collections.Concurrent;
using AgileActorsApiAggregator.Core.Interfaces;
using AgileActorsApiAggregator.Core.Models;

namespace AgileActorsApiAggregator.Infrastructure.Services;

/// <summary>
/// Thread-safe, in-memory store for per-API request statistics.
/// Uses <see cref="ConcurrentDictionary"/> and <see cref="ConcurrentBag{T}"/> to handle
/// concurrent writes from parallel API calls without locking.
/// </summary>
public sealed class InMemoryStatsStore : IStatsStore
{
    // Maps each API name to the list of all recorded response times (in milliseconds)
    private readonly ConcurrentDictionary<string, ConcurrentBag<double>> _data = new();

    public void Record(string apiName, double elapsedMs)
    {
        var bag = _data.GetOrAdd(apiName, _ => new ConcurrentBag<double>());
        bag.Add(elapsedMs);
    }

    public IReadOnlyList<ApiStats> GetAll()
    {
        var result = new List<ApiStats>();

        foreach (var (name, bag) in _data)
        {
            var times = bag.ToArray(); // snapshot to avoid mutation during iteration
            if (times.Length == 0) continue;

            int fast = 0, avg = 0, slow = 0;
            foreach (var t in times)
            {
                if (t < 100) fast++;
                else if (t <= 200) avg++;
                else slow++;
            }

            result.Add(new ApiStats(
                ApiName: name,
                TotalRequests: times.Length,
                AverageResponseMs: Math.Round(times.Average(), 2),
                Buckets: new PerformanceBuckets(fast, avg, slow)
            ));
        }

        return result;
    }

    /// <summary>
    /// Returns all recorded response times for the named API.
    /// The <paramref name="window"/> parameter is accepted for interface compatibility
    /// but is not applied — timestamps are not stored alongside response times.
    /// </summary>
    public double[] GetRecentTimes(string apiName, TimeSpan window)
    {
        return _data.TryGetValue(apiName, out var bag) ? bag.ToArray() : [];
    }
}
