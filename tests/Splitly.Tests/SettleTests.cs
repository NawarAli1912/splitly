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
        Assert.Equal(50m, transfer.Amount.Value);
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

    [Fact]
    public void PartialPayment_ShrinksTheTransfer()
    {
        var (group, alice, bob, _) = CreateTripGroup();
        group.AddExpense(alice, 100m, "Hotel", Today, [alice, bob]);
        group.RecordPayment(bob, alice, 30m, Today);

        var transfers = group.Settle();

        var transfer = Assert.Single(transfers);
        Assert.Equal(bob, transfer.FromParticipantId);
        Assert.Equal(alice, transfer.ToParticipantId);
        Assert.Equal(20m, transfer.Amount.Value);
    }

    [Fact]
    public void FullPayment_SettlesTheGroup()
    {
        var (group, alice, bob, _) = CreateTripGroup();
        group.AddExpense(alice, 100m, "Hotel", Today, [alice, bob]);
        group.RecordPayment(bob, alice, 50m, Today);

        Assert.Empty(group.Settle());
    }

    [Fact]
    public void Overpayment_FlipsTheDirection()
    {
        var (group, alice, bob, _) = CreateTripGroup();
        group.AddExpense(alice, 100m, "Hotel", Today, [alice, bob]);
        group.RecordPayment(bob, alice, 80m, Today);

        var transfers = group.Settle();

        var transfer = Assert.Single(transfers);
        Assert.Equal(alice, transfer.FromParticipantId);
        Assert.Equal(bob, transfer.ToParticipantId);
        Assert.Equal(30m, transfer.Amount.Value);
    }

    [Fact]
    public void RemovedPayment_RestoresTheDebt()
    {
        var (group, alice, bob, _) = CreateTripGroup();
        group.AddExpense(alice, 100m, "Hotel", Today, [alice, bob]);
        var payment = group.RecordPayment(bob, alice, 50m, Today).Value;

        group.RemovePayment(payment.Id);

        var transfer = Assert.Single(group.Settle());
        Assert.Equal(50m, transfer.Amount.Value);
    }

    [Fact]
    public void PaymentValidation_RejectsInvalidPayments()
    {
        var (group, alice, bob, _) = CreateTripGroup();

        Assert.Equal(
            ExpenseGroupErrors.InvalidPaymentAmount,
            group.RecordPayment(bob, alice, 0m, Today).FirstError);
        Assert.Equal(
            ExpenseGroupErrors.PaymentToSelf,
            group.RecordPayment(alice, alice, 10m, Today).FirstError);
        Assert.Equal(
            ExpenseGroupErrors.ParticipantNotFound,
            group.RecordPayment(Guid.NewGuid(), alice, 10m, Today).FirstError);
        Assert.Equal(
            ExpenseGroupErrors.PaymentNotFound,
            group.RemovePayment(Guid.NewGuid()).FirstError);
    }

    [Fact]
    public void ParticipantWithPayments_CannotBeRemoved()
    {
        var (group, alice, _, carol) = CreateTripGroup();
        group.RecordPayment(carol, alice, 10m, Today);

        Assert.Equal(
            ExpenseGroupErrors.ParticipantHasPayments,
            group.RemoveParticipant(carol).FirstError);
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
