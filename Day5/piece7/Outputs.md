# Smoke Test Outputs

**Base URL:** `https://ca-api-nb3bgcnwnlpwe.lemoncliff-d4727121.southeastasia.azurecontainerapps.io`  
**Date:** 2026-05-23

---

## GET /health

```
GET /health
→ 200 Healthy
```

Body:
```
Healthy
```

---

## POST /api/auth/login — valid

```
POST /api/auth/login
Body: {"email":"test@example.com","password":"password123"}
→ 200
```

Body:
```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.<payload>.<sig>",
  "refresh_token": "GDnswGNCBYa2cNCbvxzr0Y4Cu8CIyEzMzN1K4CDWESk=",
  "expires_in": 900
}
```

Decoded JWT payload:
```json
{"sub":"1","email":"test@example.com","scope":"quotes.write","exp":1779542386,"iss":"QuotesApi","aud":"QuotesApi"}
```

---

## POST /api/auth/login — wrong credentials

```
POST /api/auth/login
Body: {"email":"nobody@x.com","password":"wrong"}
→ 401
```

Body: *(empty)*

---

## GET /api/quotes/?page=1&size=10

```
GET /api/quotes/?page=1&size=10
→ 200
```

Body:
```json
[
  {"id":1,"author":"Marcus Aurelius","text":"The impediment to action advances action.","createdAt":"2026-05-23T11:32:57.8959321+00:00","ownerId":1},
  {"id":2,"author":"Seneca","text":"We suffer more in imagination than in reality.","createdAt":"2026-05-23T11:32:58.4135841+00:00","ownerId":1},
  {"id":3,"author":"Marcus Aurelius","text":"The impediment to action advances action.","createdAt":"2026-05-23T13:05:04.9918728+00:00","ownerId":1}
]
```

---

## GET /api/quotes/ (no params)

```
GET /api/quotes/
→ 400
```

Body: *(ASP.NET binding error — no quotes returned)*

---

## GET /api/quotes/3

```
GET /api/quotes/3
→ 200
```

Body:
```json
{"id":3,"author":"Marcus Aurelius","text":"The impediment to action advances action.","createdAt":"2026-05-23T13:05:04.9918728+00:00","ownerId":1}
```

---

## GET /api/quotes/99999

```
GET /api/quotes/99999
→ 404
```

Body: *(empty)*

---

## POST /api/quotes/ — no auth

```
POST /api/quotes/
Body: {"author":"Anon","text":"Hello world"}
→ 401
```

Body: *(empty)*

---

## POST /api/quotes/ — valid, authed

```
POST /api/quotes/
Authorization: Bearer <token>
Body: {"author":"Marcus Aurelius","text":"The impediment to action advances action."}
→ 201
```

Body:
```json
{"id":5,"author":"Marcus Aurelius","text":"The impediment to action advances action.","createdAt":"2026-05-23T13:05:04.9918728+00:00","ownerId":1}
```

---

## POST /api/quotes/ — empty author/text

```
POST /api/quotes/
Authorization: Bearer <token>
Body: {"author":"","text":""}
→ 400
```

Body:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "author": ["Author is required"],
    "text": ["Text is required"]
  },
  "traceId": "00-af3df4396e5ee729293f2e0302e6e467-f3b085978e87e2d5-01"
}
```

---

## DELETE /api/quotes/5 — own quote, authed

```
DELETE /api/quotes/5
Authorization: Bearer <token>
→ 204
```

Body: *(empty)*

---

## DELETE /api/quotes/5 — already deleted

```
DELETE /api/quotes/5
Authorization: Bearer <token>
→ 404
```

Body: *(empty)*

---

## DELETE /api/quotes/99999 — non-existent

```
DELETE /api/quotes/99999
Authorization: Bearer <token>
→ 404
```

Body: *(empty)*

---

## DELETE /api/quotes/1 — no auth

```
DELETE /api/quotes/1
→ 401
```

Body: *(empty)*

---

## POST /api/auth/refresh — valid token

```
POST /api/auth/refresh
Body: {"refresh_token":"GDnswGNCBYa2cNCbvxzr0Y4Cu8CIyEzMzN1K4CDWESk="}
→ 200
```

Body:
```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.<payload>.<sig>",
  "refresh_token": "Nq5doH2SUvaVrA8JL7q0XEK0G9gUmHKXY0677BRrAHI=",
  "expires_in": 900
}
```

---

## POST /api/auth/refresh — reused (rotated) token

```
POST /api/auth/refresh
Body: {"refresh_token":"GDnswGNCBYa2cNCbvxzr0Y4Cu8CIyEzMzN1K4CDWESk="}  ← same old token
→ 401
```

Body: *(empty — entire token family is revoked)*

---

## POST /api/auth/refresh — invalid token

```
POST /api/auth/refresh
Body: {"refresh_token":"totally-fake-token"}
→ 401
```

Body: *(empty)*

---

## POST /api/auth/logout — no auth

```
POST /api/auth/logout
Body: {"refresh_token":"Nq5doH2SUvaVrA8JL7q0XEK0G9gUmHKXY0677BRrAHI="}
→ 401
```

Body: *(empty)*

---

## POST /api/auth/logout — authed

```
POST /api/auth/logout
Authorization: Bearer <new-access-token>
Body: {"refresh_token":"Nq5doH2SUvaVrA8JL7q0XEK0G9gUmHKXY0677BRrAHI="}
→ 204
```

Body: *(empty)*

---

## POST /api/auth/refresh — after logout (revoked)

```
POST /api/auth/refresh
Body: {"refresh_token":"Nq5doH2SUvaVrA8JL7q0XEK0G9gUmHKXY0677BRrAHI="}
→ 401
```

Body: *(empty)*

---

## Bug found during testing: wrong field name → 500

Sending `refreshToken` (camelCase) instead of `refresh_token` causes a 500:

```
POST /api/auth/refresh
Body: {"refreshToken":"GDnswGNCBYa2cNCbvxzr0Y4Cu8CIyEzMzN1K4CDWESk="}
→ 500
```

Body:
```json
{"title":"Server Error","status":500,"detail":"Value cannot be null. (Parameter 's')"}
```

Root cause: `RefreshRequest` binds via `[JsonPropertyName("refresh_token")]`; sending the wrong key leaves the field `null`, which then reaches `TokenHasher.Hash(null)` → `Encoding.UTF8.GetBytes(null)` → `ArgumentNullException`. Should return 400.
