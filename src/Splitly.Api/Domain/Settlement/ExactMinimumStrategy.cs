namespace Splitly.Api.Domain.Settlement;

/// <summary>
/// The provably minimum number of transfers.
///
/// Key fact: a zero-sum subset of k participants settles internally in k−1
/// transfers, so minimum transfers = (non-zero balances) − (max number of
/// disjoint zero-sum subsets they partition into). Every extra subset found
/// saves one transfer. Finding that partition is NP-hard, hence:
/// bitmask DP over the 2^n subsets of non-zero balances for
/// n ≤ <see cref="MaxExactParticipants"/>, greedy fallback above it.
///
/// DP sketch: precompute sum(mask) for all masks; dp[mask] = max zero-sum
/// subsets covering exactly `mask`, via dp[mask] = max over zero-sum submasks
/// s of mask of dp[mask ^ s] + 1. Reconstruct the partition, then settle each
/// subset greedily (k−1 transfers each).
/// </summary>
public sealed class ExactMinimumStrategy : ISettlementStrategy
{
    public const int MaxExactParticipants = 15;

    public IReadOnlyList<Transfer> Settle(ExpenseGroup group)
    {
        // T3.6, owner: nawar — the tests in ExactMinimumTests define done.
        throw new NotImplementedException("T3.6 — bitmask DP over zero-sum subsets");
    }
}
