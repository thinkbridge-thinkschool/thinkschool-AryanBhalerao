# KQL Queries

## Slowest 10 requests in the last hour

```kql
requests
| where timestamp > ago(1h)
| project timestamp, name, url, duration, resultCode, success, operation_Id, customDimensions
| top 10 by duration desc
```
