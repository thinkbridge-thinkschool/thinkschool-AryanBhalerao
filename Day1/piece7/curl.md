# Collection invariant violation — duplicate QuoteId (400 Bad Request)

trying to add "quoteId": 42 when it already exists,

curl -s -X POST http://localhost:5051/api/collections/1/items \
  -H "Content-Type: application/json" \
  -d '{"quoteId":42}'
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Quote 42 is already in this collection.",
  "traceId": "00-a2eef83849e0076ec26bf0d2ac9b60e0-fe34f7a053b253cf-00"
}
```