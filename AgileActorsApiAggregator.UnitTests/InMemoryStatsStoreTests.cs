using AgileActorsApiAggregator.Infrastructure.Services;
using FluentAssertions;

namespace AgileActorsApiAggregator.Tests;

/// <summary>
/// Unit tests for <see cref="InMemoryStatsStore"/>.
/// Verifies request recording, performance bucket classification, and thread safety.
/// </summary>
public class InMemoryStatsStoreTests
{
    private readonly InMemoryStatsStore _store = new();

    [Fact]
    public void Record_And_GetAll_ReturnCorrectTotals()
    {
        _store.Record("API_A", 50);
        _store.Record("API_A", 100);
        _store.Record("API_A", 150);
        _store.Record("API_A", 200);
        _store.Record("API_A", 250);
        _store.Record("API_A", 300);

        var stats = _store.GetAll();
        stats.Should().HaveCount(1);

        var s = stats[0];
        s.ApiName.Should().Be("API_A");
        s.TotalRequests.Should().Be(6);
    }

    [Fact]
    public void Buckets_AreClassifiedCorrectly()
    {
        _store.Record("API_B", 25);   // fast
        _store.Record("API_B", 50);   // fast
        _store.Record("API_B", 75);   // fast
        _store.Record("API_B", 100);  // average
        _store.Record("API_B", 150);  // average
        _store.Record("API_B", 200);  // average
        _store.Record("API_B", 300);  // slow
        _store.Record("API_B", 350);  // slow
        _store.Record("API_B", 400);  // slow

        var stats = _store.GetAll().First(s => s.ApiName == "API_B");

        stats.Buckets.Fast.Should().Be(3);
        stats.Buckets.Average.Should().Be(3);
        stats.Buckets.Slow.Should().Be(3);
    }


    [Fact]
    public void Record_IsConcurrentlySafe()
    {
        const int threadCount = 20;
        const int recordsPerThread = 100;

        Parallel.For(0, threadCount, _ =>
        {
            for (int i = 0; i < recordsPerThread; i++)
                _store.Record("API_C", 50);
        });

        var stats = _store.GetAll().First(s => s.ApiName == "API_C");
        stats.TotalRequests.Should().Be(threadCount * recordsPerThread);
    }
}
