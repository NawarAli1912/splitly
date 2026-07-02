using Splitly.Api.Domain;

namespace Splitly.Tests;

public class SettleTests
{
    [Fact]
    public void ExpenseSplitTwoWays_ProducesSingleTransfer()
    {
        var (group, alice, bob, _) = CreateTripGroup();
        group.AddExpense(alice, 100m, "Hotel", Today, [alice, bob]);

        var transfers = group.Settle();

        var transfer = Assert.Single(transfers);
        Assert.Equal(bob, transfer.FromParticipantId);
        Assert.Equal(alice, transfer.ToParticipantId);
        Assert.Equal(50m, transfer.Amount);
    }

    [Fact]
    public void ThreeParticipants_SettleInAtMostTwoTransfers()
    {
        var (group, alice, bob, carol) = CreateTripGroup();
        group.AddExpense(alice, 90m, "Dinner", Today, [alice, bob, carol]);
        group.AddExpense(bob, 30m, "Taxi", Today, [alice, bob, carol]);

        var transfers = group.Settle();

        Assert.True(transfers.Count <= 2);
        AssertEveryoneSettled(group, transfers);
    }

    [Fact]
    public void IndivisibleAmount_LosesNoCent()
    {
        var (group, alice, bob, carol) = CreateTripGroup();
        group.AddExpense(alice, 100m, "Groceries", Today, [alice, bob, carol]);

        var transfers = group.Settle();

        AssertEveryoneSettled(group, transfers);
    }

    [Fact]
    public void NoExpenses_ProducesNoTransfers()
    {
        var (group, _, _, _) = CreateTripGroup();

        Assert.Empty(group.Settle());
    }

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static (ExpenseGroup Group, Guid Alice, Guid Bob, Guid Carol) CreateTripGroup()
    {
        var group = ExpenseGroup.Create("Trip", "EUR").Value;
        var alice = group.AddParticipant("Alice").Value.Id;
        var bob = group.AddParticipant("Bob").Value.Id;
        var carol = group.AddParticipant("Carol").Value.Id;

        return (group, alice, bob, carol);
    }

    private static void AssertEveryoneSettled(ExpenseGroup group, IReadOnlyList<Transfer> transfers)
    {
        var balances = group.Participants.ToDictionary(p => p.Id, _ => 0m);

        foreach (var expense in group.Expenses)
        {
            balances[expense.PaidById] += expense.Amount;
            var share = expense.Amount / expense.SplitAmong.Count;

            foreach (var participantId in expense.SplitAmong)
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
            Assert.True(Math.Abs(balance) < 0.01m, $"Participant {participantId} left with balance {balance}");
        }
    }
}
