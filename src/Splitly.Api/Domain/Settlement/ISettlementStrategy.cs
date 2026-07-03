namespace Splitly.Api.Domain.Settlement;

public interface ISettlementStrategy
{
    IReadOnlyList<Transfer> Settle(ExpenseGroup group);
}
