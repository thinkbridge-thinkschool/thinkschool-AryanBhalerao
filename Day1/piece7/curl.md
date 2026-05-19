# Collection invariant violation — duplicate QuoteId (400 Bad Request)

1. Create a collection and add QuoteId 42 to it.

```bash
curl -s -X POST http://localhost:5051/api/collections \
  -H "Content-Type: application/json" \
  -d '{"name":"My Favourites","ownerId":"user1"}'
```

```json
{"id":1,"name":"My Favourites","ownerId":"user1","items":[]}
```

```bash
curl -s -X POST http://localhost:5051/api/collections/1/items \
  -H "Content-Type: application/json" \
  -d '{"quoteId":42}'
```

```
HTTP 204 No Content
```

2. Trying adding the same QuoteId a second time:

```bash
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
