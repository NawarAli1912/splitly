using BenchmarkDotNet.Attributes;
using Splitly.Api.Domain;
using Splitly.Api.Domain.Settlement;

namespace Splitly.Benchmarks;

/// <summary>
/// Greedy vs exact vs direct-payback by group size. Participants = 15 is the
/// worst case for the exact strategy (largest bitmask DP before the greedy
/// fallback kicks in); 20 shows the fallback path costs the same as greedy.
/// </summary>
[MemoryDiagnoser]
public class SettlementBenchmarks
{
    [Params(6, 10, 15, 20)]
    public int Participants { get; set; }

    private ExpenseGroup _group = null!;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42); // fixed seed: same workload every run
        _group = ExpenseGroup.Create("Bench", "EUR").Value;

        var people = Enumerable.Range(0, Participants)
            .Select(i => _group.AddParticipant($"P{i}").Value.Id)
            .ToList();

        for (var i = 0; i < Participants * 3; i++)
        {
            var payer = people[random.Next(people.Count)];
            var split = people.Where(_ => random.Next(2) == 0).ToList();
            if (split.Count == 0)
            {
                split.Add(people[random.Next(people.Count)]);
            }

            _group.AddExpense(
                payer,
                random.Next(100, 20000) / 100m,
                $"e{i}",
                DateOnly.FromDateTime(DateTime.UtcNow),
                split);
        }
    }

    [Benchmark(Baseline = true)]
    public IReadOnlyList<Transfer> Greedy() => _group.Settle(new MinimumTransfersStrategy());

    [Benchmark]
    public IReadOnlyList<Transfer> Exact() => _group.Settle(new ExactMinimumStrategy());

    [Benchmark]
    public IReadOnlyList<Transfer> DirectPayback() => _group.Settle(new DirectPaybackStrategy());
}
