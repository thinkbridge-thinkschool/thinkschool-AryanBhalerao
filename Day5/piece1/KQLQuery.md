## Find slow endpoints in Application Insights

```kql
requests
| where timestamp > ago(24h)
| where duration > 1000
| summarize
    count          = count(),
    p50_ms         = percentile(duration, 50),
    p95_ms         = percentile(duration, 95),
    p99_ms         = percentile(duration, 99)
    by name, url
| order by p95_ms desc
```
