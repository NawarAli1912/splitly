# Splitly

Split group expenses and settle up with the minimum number of payments.

Add expenses for a trip or a shared flat, and Splitly tells you exactly who pays whom — not 40 back-and-forth transfers, but the minimal set (at most n−1 for n people).

## What this demonstrates

- **Graph algorithms** — settling a group is a min cash flow problem, solved greedily over net balances
- **Correct money handling** — `decimal` arithmetic, deterministic remainder distribution (no lost cents)
- **ASP.NET Core API design** — minimal API, OpenAPI
- **Test-first core** — the settlement algorithm is specified by tests before implementation

## Run

```bash
dotnet test
dotnet run --project src/Splitly.Api
```

## Status

Early days. The settlement algorithm spec lives in `tests/Splitly.Tests/SettlementTests.cs`; implementation in progress.
