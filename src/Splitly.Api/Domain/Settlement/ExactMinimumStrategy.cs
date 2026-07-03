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
/// DP: sum(mask) is precomputed incrementally; dp[mask] = max zero-sum chunks
/// partitioning exactly `mask`, via dp[mask] = max over zero-sum submasks s
/// (fixed to contain mask's lowest bit, so each partition is counted once) of
/// dp[mask ^ s] + 1. Reconstruction follows the recorded chunk choices; each
/// chunk is then settled greedily, which needs exactly k−1 transfers because
/// a maximal partition has no splittable chunk. Submask enumeration makes the
/// whole thing O(3^n) — ~14M steps at the n = 15 cap.
/// </summary>
public sealed class ExactMinimumStrategy : ISettlementStrategy
{
    public const int MaxExactParticipants = 15;

    public IReadOnlyList<Transfer> Settle(ExpenseGroup group)
    {
        var balances = group.NetBalances()
            .Where(b => !b.Value.IsZero)
            .ToList();

        if (balances.Count > MaxExactParticipants)
        {
            // ponytail: exponential DP is capped; above the cap the greedy answer
            // is at most a few transfers worse and always correct.
            return MinimumTransfersStrategy.GreedyMatch(balances);
        }

        var n = balances.Count;

        if (n == 0)
        {
            return [];
        }

        var cents = balances.Select(b => b.Value.Cents).ToArray();
        var full = (1 << n) - 1;

        // sum of each subset, built from the subset without its lowest bit
        var sums = new long[full + 1];
        for (var mask = 1; mask <= full; mask++)
        {
            var lowBit = mask & -mask;
            sums[mask] = sums[mask ^ lowBit] + cents[int.TrailingZeroCount(lowBit)];
        }

        // dp[mask] = max zero-sum chunks partitioning mask exactly (-1 = not partitionable)
        var dp = new int[full + 1];
        var chunkChoice = new int[full + 1];
        Array.Fill(dp, -1);
        dp[0] = 0;

        for (var mask = 1; mask <= full; mask++)
        {
            if (sums[mask] != 0)
            {
                continue; // only zero-sum masks can be unions of zero-sum chunks
            }

            var lowBit = mask & -mask;

            // enumerate submasks of mask that contain its lowest bit — the chunk
            // holding that participant — so each partition is generated once
            for (var chunk = mask; chunk != 0; chunk = (chunk - 1) & mask)
            {
                if ((chunk & lowBit) == 0 || sums[chunk] != 0 || dp[mask ^ chunk] < 0)
                {
                    continue;
                }

                if (dp[mask ^ chunk] + 1 > dp[mask])
                {
                    dp[mask] = dp[mask ^ chunk] + 1;
                    chunkChoice[mask] = chunk;
                }
            }
        }

        // balances always sum to zero, so the full mask partitions into ≥ 1 chunk
        List<Transfer> transfers = [];

        for (var mask = full; mask != 0; mask ^= chunkChoice[mask])
        {
            var chunk = chunkChoice[mask];
            var chunkBalances = balances.Where((_, i) => (chunk & (1 << i)) != 0);
            transfers.AddRange(MinimumTransfersStrategy.GreedyMatch(chunkBalances));
        }

        return transfers;
    }
}
