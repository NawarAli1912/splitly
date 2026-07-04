# Settlement strategy benchmarks

```bash
dotnet run -c Release --project benchmarks/Splitly.Benchmarks -- --filter '*'
```

Synthetic group per size (fixed seed): n participants, 3n random expenses with random
splits. `Participants = 15` is the exact strategy's worst case — the largest bitmask DP
(2¹⁵ masks) before the greedy fallback takes over; 20 exercises the fallback.

## Results — Apple M5, .NET 10, ShortRun (2026-07-04)

| Method        | Participants |       Mean | Ratio | Allocated |
|---------------|-------------:|-----------:|------:|----------:|
| Greedy        |            6 |   1.847 μs |  1.00 |  10.45 KB |
| Exact         |            6 |   2.073 μs |  1.12 |  12.23 KB |
| DirectPayback |            6 |   3.334 μs |  1.80 |  16.16 KB |
| Greedy        |           10 |   3.937 μs |  1.00 |  21.36 KB |
| Exact         |           10 |   6.207 μs |  1.58 |  38.30 KB |
| DirectPayback |           10 |   8.420 μs |  2.14 |  35.30 KB |
| Greedy        |           15 |   8.275 μs |  1.00 |  40.22 KB |
| Exact         |           15 | 109.013 μs | 13.17 | 553.57 KB |
| DirectPayback |           15 |  19.315 μs |  2.33 |  75.80 KB |
| Greedy        |           20 |  13.892 μs |  1.00 |  67.53 KB |
| Exact         |           20 |  13.681 μs |  0.98 |  68.14 KB |
| DirectPayback |           20 |  36.929 μs |  2.66 | 138.27 KB |

## How to read it

- **Exact is effectively free at real group sizes.** At n ≤ 10 it costs within ~1.6× of
  greedy in the single-digit-microsecond range.
- **The worst case is bounded and still negligible**: 109 μs and ~550 KB at n = 15 —
  the full 2¹⁵-mask DP — is three orders of magnitude below any network hop. The n ≤ 15
  cap exists to keep it that way: at n = 16 the DP would double, at n = 25 it would be
  seconds.
- **The fallback costs nothing**: at n = 20 exact and greedy are statistically identical
  (ratio 0.98) — above the cap, exact *is* greedy plus a list scan.
- DirectPayback's 2–2.7× comes from building the pairwise debt matrix per expense share;
  it buys explainability, not speed, which is exactly its trade.
