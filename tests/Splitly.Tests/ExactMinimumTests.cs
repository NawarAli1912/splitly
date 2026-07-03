using Splitly.Api.Domain;
using Splitly.Api.Domain.Settlement;

namespace Splitly.Tests;

/// <summary>
/// Red tests for T3.6 — these define "done" for ExactMinimumStrategy.
/// </summary>
public class ExactMinimumTests
{
    [Fact]
    public void TwoIndependentClusters_BeatsGreedy()
    {
        // Balances: A +8, B −4, C −4 | D +6, E −3, F −3.
        // Two zero-sum subsets of size 3 → exact minimum is 2+2 = 4 transfers.
        // Greedy largest-with-largest crosses the clusters and needs 5.
        var group = ExpenseGroup.Create("Clusters", "EUR").Value;
        var a = group.AddParticipant("A").Value.Id;
        var b = group.AddParticipant("B").Value.Id;
        var c = group.AddParticipant("C").Value.Id;
        var d = group.AddParticipant("D").Value.Id;
        var e = group.AddParticipant("E").Value.Id;
        var f = group.AddParticipant("F").Value.Id;
        group.AddExpense(a, 8m, "Cluster 1", Today, [b, c]);
        group.AddExpense(d, 6m, "Cluster 2", Today, [e, f]);

        var greedy = group.Settle(new MinimumTransfersStrategy());
        var exact = group.Settle(new ExactMinimumStrategy());

        Assert.Equal(5, greedy.Count); // documents why greedy alone isn't enough
        Assert.Equal(4, exact.Count);
        AssertEveryoneSettled(group, exact);
    }

    [Fact]
    public void ExactNeverExceedsGreedy_AndAlwaysSettles()
    {
        var random = new Random(20260704); // seeded: failures must reproduce

        for (var round = 0; round < 25; round++)
        {
            var group = ExpenseGroup.Create($"Random {round}", "EUR").Value;
            var people = Enumerable.Range(0, random.Next(3, 9))
                .Select(i => group.AddParticipant($"P{i}").Value.Id)
                .ToList();

            for (var i = 0; i < random.Next(2, 12); i++)
            {
                var payer = people[random.Next(people.Count)];
                var split = people.Where(_ => random.Next(2) == 0).ToList();
                if (split.Count == 0) split.Add(people[random.Next(people.Count)]);
                group.AddExpense(payer, random.Next(100, 20000) / 100m, $"e{i}", Today, split);
            }

            var greedy = group.Settle(new MinimumTransfersStrategy());
            var exact = group.Settle(new ExactMinimumStrategy());

            Assert.True(exact.Count <= greedy.Count,
                $"round {round}: exact {exact.Count} > greedy {greedy.Count}");
            AssertEveryoneSettled(group, exact);
        }
    }

    [Fact]
    public void AboveMaxExactParticipants_FallsBackToGreedy()
    {
        var group = ExpenseGroup.Create("Big", "EUR").Value;
        var people = Enumerable.Range(0, 20)
            .Select(i => group.AddParticipant($"P{i}").Value.Id)
            .ToList();

        for (var i = 0; i < people.Count - 1; i++)
        {
            group.AddExpense(people[i], 10m + i, $"e{i}", Today, [people[i + 1]]);
        }

        var greedy = group.Settle(new MinimumTransfersStrategy());
        var exact = group.Settle(new ExactMinimumStrategy());

        Assert.Equal(greedy.Count, exact.Count); // no DP at n=20 — same answer, no blowup
        AssertEveryoneSettled(group, exact);
    }

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static void AssertEveryoneSettled(ExpenseGroup group, IReadOnlyList<Transfer> transfers)
    {
        var balances = group.Participants.ToDictionary(p => p.Id, _ => Money.Zero);

        foreach (var expense in group.Expenses)
        {
            balances[expense.PaidById] += expense.Amount;

            foreach (var (participantId, share) in expense.Shares())
            {
                balances[participantId] -= share;
            }
        }

        foreach (var transfer in transfers)
        {
            balances[transfer.FromParticipantId] += transfer.Amount;
            balances[transfer.ToParticipantId] -= transfer.Amount;
        }

        foreach (var (participantId, balance) in balances)
        {
            Assert.True(balance.IsZero, $"Participant {participantId} left with balance {balance}");
        }
    }
}
