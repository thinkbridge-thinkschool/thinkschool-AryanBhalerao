# App Insights KQL Observations

## Query: requests | summarize count(), p50, p99 by name (last 30 min)

| name | count | p50 (ms) | p99 (ms) |
|---|---|---|---|
| POST /api/auth/login | 6 | 662.52 | 1449.03 |
| GET /health | 11 | 0.48 | 279.33 |
| POST /api/quotes/ | 4 | 4.19 | 189.26 |
| POST /api/auth/refresh | 2 | 17.93 | 62.20 |
| GET /api/quotes/ | 4 | 2.61 | 25.36 |
| GET /api/quotes/{id:int} | 6 | 1.94 | 9.40 |

## Observation

`GET /health` was the most surprising. 

- **No logic, yet slowest tail.** The health endpoint has zero application work — no DB call, no auth, no serialization. `POST /api/quotes/` does a DB write and still has a lower p99 (189 ms vs 279 ms).
- **575x gap between p50 and p99.** p50 is 0.48 ms (correct for a no-op), but p99 blows out to 279 ms. Every other endpoint has a reasonable spread; only health collapses like this.
- **Single replica + min=1 scale config.** The Bicep sets `minReplicas: 1`. Azure Container Apps can park the single replica in a low-priority CPU slot between requests. A health probe hitting it during a scheduler yield takes the full OS wakeup cost — the app code runs in under 1 ms, but the container doesn't get CPU for ~279 ms.
- **Practical risk.** Load balancers and readiness probes often have tight timeouts (100–200 ms). A p99 of 279 ms on the health route means roughly 1 in 100 probes could time out and trigger an unnecessary restart or mark the replica unhealthy.
