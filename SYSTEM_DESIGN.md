# Splitly — System Design & Capacity Plan

How Splitly serves **1M registered users at ~500 requests/second peak**, and the staged
plan to 10M and beyond. Numbers below are an engineering estimate: an explicit load model
plus measured single-node behavior — each stage names the load test that must validate it
before we rely on it.

## 1. Architecture today

```
Browser (Vue SPA) ──► nginx (static + /api proxy) ──► ASP.NET Core API ──► PostgreSQL
```

- **Stateless API.** No sessions, no server-side per-user state; identity is a claimed
  participant id held by the client. Any request can hit any replica — horizontal scaling
  is a replica count, not a redesign.
- **The aggregate is the scaling unit.** Every endpoint is scoped to one `ExpenseGroup`;
  there are zero cross-group queries in the API. One group = one transaction boundary =
  one cache key = (later) one shard key.
- **Settlement is compute, not storage.** Transfers are derived on read from net balances
  (O(n log n) greedy, or capped O(3^n) exact for n ≤ 15). Groups are human-sized —
  a 50-person group is an outlier — so settlement cost per request is microseconds.

## 2. Load model — where "1M users, 500 rps" comes from

Assumptions (Splitwise-class usage, deliberately on the heavy side):

| Assumption | Value | Rationale |
|---|---|---|
| Registered users | 1,000,000 | target claim |
| Daily active ratio | 7.5% → 75k DAU | expense apps are bursty (trips, month-end), not daily-habit |
| Requests per active user per day | 20 | open group ≈ 4 calls (group, expenses, payments, settlement) × a few visits + 1–2 writes |
| Peak-to-average factor | 20× | evening + weekend spikes, single dominant timezone |

Math: 75k DAU × 20 req = **1.5M requests/day** ≈ **17 rps average** → ×20 peak ≈ **350 rps
peak**. Design target **500 rps** gives ~40% headroom over the modeled peak.

Read:write is roughly 85:15 — writes are one INSERT into an indexed table; reads load one
group's rows and compute in memory.

**Measured baseline** (honest scope: smoke, not a load test): on a dev laptop, single
docker-compose node, the Postman/newman suite over a seeded group (8 participants,
50+ expenses) sustains the full hot path — settlement in all strategies, paginated list,
concurrent-ish writes — at **2–18 ms per request**. That is ~2 orders of magnitude below
the 150–250 ms SLO targets, which is the headroom that makes the Stage 1 claim credible.
Validation gate: a k6/Postman performance run at 500 rps against a staging deployment
before the claim goes in front of anyone.

## 3. Data growth

150k new expenses/day at peak activity (75k DAU × 2 writes) ≈ **55M rows/year**, each
~250 bytes with its split array → **~15 GB/year** plus indexes. A single managed Postgres
handles this for years; time-partitioning `expenses` by month is available long before it
hurts.

## 4. Bottlenecks, in the order they will actually appear

1. **`expenses` scan on list/settlement** — the deferred index
   `expenses(expense_group_id, spent_on)` is the first thing to land when p95 moves
   (already parked in DECISIONS with this trigger).
2. **Connection exhaustion** once API replicas multiply → PgBouncer (transaction pooling)
   in front of Postgres.
3. **Settlement recompute on every read** — cheap per group but the top read path at
   scale → per-group response cache (settlement + expense page) keyed by
   `(groupId, version)`, where version bumps on any write to the group; ETags fall out of
   the same key for free.
4. **Whole-aggregate loads** — handlers `Include` all participants/expenses/payments;
   correct at human group sizes, wasteful at pathological ones → cap participants per
   group (product decision, e.g. 50) rather than re-architect for a case that shouldn't
   exist.

## 5. Scaling stages

### Stage 0 — today (demo)
Single node, docker compose, migrations auto-apply. Fine for exactly one machine's worth
of trust.

### Stage 1 — the 1M-user / 500 rps claim
- 2–3 stateless API replicas behind a load balancer (any container platform; zero code
  changes required — this is the payoff of statelessness).
- Managed PostgreSQL (4 vCPU class), PgBouncer, automated backups + PITR.
- CDN for the static SPA; only `/api` reaches the origin.
- The `expenses(expense_group_id, spent_on)` index.
- **Prerequisites for being public at all**: real auth (personal links become server-side
  tokens — the URL shape already survives this, see DECISIONS #20), rate limiting,
  OpenTelemetry traces/metrics + dashboards.
- SLOs: 99.9% availability, p95 read < 150 ms, p95 write < 250 ms.
- Exit criterion: sustained 500 rps load test green against staging.

### Stage 2 — 10M users / ~5k rps
- Read replicas; settlement/read cache from bottleneck #3 takes the hot reads off the
  primary entirely (cache hit = no DB touch).
- Async side-features (debt reminders, activity feed) leave the request path: outbox
  table → queue → workers. The write path stays one INSERT.
- `split_among` array column → join table *if and when* per-participant queries appear
  (Insights today is client-side; the array is the right call until then).

### Stage 3 — 100M users / ~50k rps
- **Shard by `group_id`.** This is why the aggregate discipline matters: every query in
  the system is group-scoped, so sharding is routing, not rewriting. Citus-style
  distributed Postgres or app-level routing both work; personal groups collocate freely.
- Identity/accounts split into their own service only when auth traffic demands it.
- Multi-region: active-passive first (EU primary), CDN already global; active-active
  reads via regional replicas if latency data says so.

### Non-goals (deliberate)
Microservices, Kubernetes, event sourcing, and multi-region active-active writes are all
absent from Stage 1 on purpose: the load model doesn't justify their operational cost,
and every stage above shows the seam where each would slot in if the model is wrong.

## 6. What to watch

The plan is falsified or confirmed by four numbers on a dashboard: p95 per endpoint,
DB CPU + connection count, cache hit rate (Stage 2+), and error rate. Each stage's
trigger is one of these drifting toward its SLO — not a date on a roadmap.
