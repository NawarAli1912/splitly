# Splitly

[![ci](https://github.com/NawarAli1912/splitly/actions/workflows/ci.yml/badge.svg)](https://github.com/NawarAli1912/splitly/actions/workflows/ci.yml)

Split group expenses and settle up with the minimum number of payments.

Add expenses for a trip or a shared flat, and Splitly tells you exactly who pays whom — not 40 back-and-forth transfers, but the minimal set (at most n−1 for n people).

## What this demonstrates

- **Graph algorithms** — settlement is a min cash flow problem: net balances (paid − consumed), then greedy matching over two max-heaps, O(n log n)
- **Correct money handling** — a `Money` type over integer cents; division distributes remainder cents deterministically, no cent is ever lost
- **Rich domain model** — every invariant lives inside the `ExpenseGroup` aggregate; invalid state is unrepresentable from outside
- **REST API discipline** — request/response contracts, thin controllers, one handler per use case, every error an RFC 7807 `problem+json` body
- **Full-stack integration tests** — Testcontainers Postgres + `WebApplicationFactory`, the whole journey through real HTTP

## API

```
POST   /groups
GET    /groups/{id}
DELETE /groups/{id}
POST   /groups/{id}/participants
DELETE /groups/{id}/participants/{participantId}
POST   /groups/{id}/expenses
GET    /groups/{id}/expenses?page=1&pageSize=20
DELETE /groups/{id}/expenses/{expenseId}
GET    /groups/{id}/settlement
```

Errors follow one contract: `400` validation (field-level details), `404` not found, `409` conflict — always `application/problem+json`.

## Run

```bash
docker compose up -d      # Postgres 17
dotnet run --project src/Splitly.Api
```

## Test

```bash
dotnet test               # unit + integration (integration tests start their own Postgres via Testcontainers)
```

## Stack

ASP.NET Core (.NET 10) · EF Core + PostgreSQL · ErrorOr · xUnit + Testcontainers
