# curl — Entra ID token hitting QuotesApi

## Token retrieved in Azure Cloud Shell
TOKEN=$(az account get-access-token \
  --resource api://625aa1cf-d69d-4267-a1cb-8849773ac822 \
  --query accessToken \
  --output tsv)

## curl Command

```bash
TOKEN="eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Ilh0LW83aERicHVwQXotWlBtNkh4Q0ZXUzNjSSIsImtpZCI6Ilh0LW83aERicHVwQXotWlBtNkh4Q0ZXUzNjSSJ9.eyJhdWQiOiJhcGk6Ly82MjVhYTFjZi1kNjlkLTQyNjctYTFjYi04ODQ5NzczYWM4MjIiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC83ZTM5NGZjOC00Yjg2LTRjZmUtODEwZS00M2Y4NmY4YmVjNDcvIiwiaWF0IjoxNzc5MzQwMTc3LCJuYmYiOjE3NzkzNDAxNzcsImV4cCI6MTc3OTM0NTY3MSwiYWNyIjoiMSIsImFpbyI6IkFYUUFpLzhjQUFBQWYwRXNxS3VoU3JRRURJSEtRL250eWVDR0ZOZlNOdndPWjNZdkd3NHQwNmIyZTlVZDZpa1kveXRKM0FNL2FHNDZpRUUxNzRGYVZIc2JCYngxT2wwRUdaZlB0cFB5dkh0cG92VjF5NTVyK0JOQmVWd2JOMUlqcEE4bnZXWHVaYlJHaldvMjFGQ1FZNDRBdVNVKzFIVUZMUT09IiwiYW1yIjpbInB3ZCIsIm1mYSJdLCJhcHBpZCI6IjA0YjA3Nzk1LThkZGItNDYxYS1iYmVlLTAyZjllMWJmN2I0NiIsImFwcGlkYWNyIjoiMCIsImZhbWlseV9uYW1lIjoiQXJ5YW4gQmhhbGVyYW8iLCJnaXZlbl9uYW1lIjoiMi4wMkUrMTEiLCJpcGFkZHIiOiIyMC4yMDcuMTkyLjE4MCIsIm5hbWUiOiIyMDIxMDEwNDAwNzUgQXJ5YW4gQmhhbGVyYW8iLCJvaWQiOiJlOGVlYzI5MC00YzM4LTQ2MGYtYmY2NS1lMGZlMzExYzc5N2YiLCJyaCI6IjEuQVZVQXlFODVmb1pMX2t5QkRrUDRiNHZzUjgtaFdtS2QxbWRDb2N1SVNYYzZ5Q0lBQU0xVkFBLiIsInNjcCI6ImFjY2Vzc19hc191c2VyIiwic2lkIjoiMDA0Zjg3N2EtZGJhYi1kNGEzLWYzMWUtYzUwYjQwNmVmZjFlIiwic3ViIjoiTXhHRGZ5bndwTzdrOEhXdEUtcU1paFhpS2lWTnJHaExlSWdaNVhUVHByNCIsInRpZCI6IjdlMzk0ZmM4LTRiODYtNGNmZS04MTBlLTQzZjg2ZjhiZWM0NyIsInVuaXF1ZV9uYW1lIjoiMjAyMTAxMDQwMDc1QG1zdGVhbXMubWl0YW9lLmFjLmluIiwidXBuIjoiMjAyMTAxMDQwMDc1QG1zdGVhbXMubWl0YW9lLmFjLmluIiwidXRpIjoiT0tVOS1xOGZkRWF4c2FqemRIME5BQSIsInZlciI6IjEuMCIsInhtc19mdGQiOiJMZnRYbUFNc2JUbG5UVTZlTmVEUHJDdnU1R21YMmw1c1d5OWdnV0NiRlNBQllYTnBZWE52ZFhSb1pXRnpkQzFrYzIxeiJ9.Qozm-7sysxpX1NmalXnbEWO5rjazjxuwvgzLehhpY273ejCTQV-ICJYAKPhO9T_SnaKTNErhC8Gq2WfmBQMRySJM6DhPcFCvecHrc5rT-UtAUJSAfo3adEO34d_tsFd1dRalJtjGKgA1tGlKJZPE8jbnxkSbIC5ob7UagF2LrgTz762YPys8VMC07YyTE-SdwZBStKN0CGmJDw-OOzbe86lDf4zMRUQ0RpgttOJeP6cCw2qpP4e7Q3BkXyo5iuvJUIt-jSybgq-RiCyG_sNaknKgiyPMkFBIDmahvFx7kyPdyu4u2URF-_KTWfCvIq9Mz1PRwkatMB5HsXMnYLMbcQ"

curl -s -i \
  -X POST http://localhost:5051/api/quotes \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"author":"Richard Feynman","text":"The first principle is that you must not fool yourself and you are the easiest person to fool."}'
```

## Output
```
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Date: Thu, 21 May 2026 05:53:14 GMT
Server: Kestrel
Location: /api/quotes/6
Transfer-Encoding: chunked

{"id":6,"author":"Richard Feynman","text":"The first principle is that you must not fool yourself and you are the easiest person to fool.","isDeleted":false,"createdAt":"2026-05-21T05:53:14.5143562Z"}
```
