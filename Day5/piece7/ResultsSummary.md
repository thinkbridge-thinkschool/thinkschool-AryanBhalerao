# Smoke-Test Results

**Base URL:** `https://ca-api-nb3bgcnwnlpwe.lemoncliff-d4727121.southeastasia.azurecontainerapps.io`  
**Date:** 2026-05-23  
**Test credentials:** `test@example.com` / `password123`

---

## Health

| # | Check | Request | Expected | Actual | Result |
|---|-------|---------|----------|--------|--------|
| 1 | Health endpoint | `GET /health` | 200 `Healthy` | 200 `Healthy` | PASS |

---

## Auth — Login

| # | Check | Request | Expected | Actual | Result |
|---|-------|---------|----------|--------|--------|
| 2 | Login with valid credentials | `POST /api/auth/login` `{"email":"test@example.com","password":"password123"}` | 200 + tokens | 200 `{"access_token":"…","refresh_token":"…","expires_in":900}` | PASS |
| 3 | Login with wrong password | `POST /api/auth/login` `{"email":"nobody@x.com","password":"wrong"}` | 401 | 401 | PASS |

Login response shape:
```json
{
  "access_token": "<JWT>",
  "refresh_token": "<base64>",
  "expires_in": 900
}
```

---

## Quotes — Anonymous reads

| # | Check | Request | Expected | Actual | Result |
|---|-------|---------|----------|--------|--------|
| 4 | List quotes (paginated) | `GET /api/quotes/?page=1&size=10` | 200 array | 200 JSON array | PASS |
| 5 | List quotes without query params | `GET /api/quotes/` | 400 | 400 | PASS |
| 6 | Get existing quote by ID | `GET /api/quotes/3` | 200 | 200 `{"id":3,"author":"Marcus Aurelius",…}` | PASS |
| 7 | Get non-existent quote | `GET /api/quotes/99999` | 404 | 404 | PASS |

List response shape (flat array, no pagination wrapper):
```json
[
  {"id":1,"author":"Marcus Aurelius","text":"The impediment to action advances action.","createdAt":"2026-05-23T11:32:57.89Z","ownerId":1},
  …
]
```

---

## Quotes — Authenticated writes

| # | Check | Request | Expected | Actual | Result |
|---|-------|---------|----------|--------|--------|
| 8 | Create quote without auth | `POST /api/quotes/` (no Bearer) | 401 | 401 | PASS |
| 9 | Create quote with valid auth + body | `POST /api/quotes/` Bearer + `{"author":"Marcus Aurelius","text":"…"}` | 201 | 201 `{"id":5,…,"ownerId":1}` | PASS |
| 10 | Create quote with empty author/text | `POST /api/quotes/` Bearer + `{"author":"","text":""}` | 400 ValidationProblem | 400 `{"errors":{"author":["Author is required"],"text":["Text is required"]},…}` | PASS |

---

## Quotes — Delete

| # | Check | Request | Expected | Actual | Result |
|---|-------|---------|----------|--------|--------|
| 11 | Delete own quote (authed) | `DELETE /api/quotes/5` Bearer | 204 | 204 | PASS |
| 12 | Delete already-deleted quote | `DELETE /api/quotes/5` Bearer | 404 | 404 | PASS |
| 13 | Delete non-existent quote | `DELETE /api/quotes/99999` Bearer | 404 | 404 | PASS |
| 14 | Delete without auth | `DELETE /api/quotes/1` (no Bearer) | 401 | 401 | PASS |

> **Not tested:** `DELETE` a quote owned by a *different* user (→ should return 403). Only one user exists in the deployed DB, so this path could not be exercised.

---

## Auth — Refresh token

> **Important:** the request body field is `refresh_token` (snake_case), not `refreshToken`.

| # | Check | Request | Expected | Actual | Result |
|---|-------|---------|----------|--------|--------|
| 15 | Refresh with valid token | `POST /api/auth/refresh` `{"refresh_token":"<token>"}` | 200 + new tokens | 200 `{"access_token":"…","refresh_token":"…","expires_in":900}` | PASS |
| 16 | Refresh reuse (rotated token) | `POST /api/auth/refresh` same old token after rotation | 401 | 401 | PASS |
| 17 | Refresh with invalid token | `POST /api/auth/refresh` `{"refresh_token":"totally-fake-token"}` | 401 | 401 | PASS |

---

## Auth — Logout

| # | Check | Request | Expected | Actual | Result |
|---|-------|---------|----------|--------|--------|
| 18 | Logout without auth | `POST /api/auth/logout` (no Bearer) `{"refresh_token":"…"}` | 401 | 401 | PASS |
| 19 | Logout with valid Bearer + refresh token | `POST /api/auth/logout` Bearer + `{"refresh_token":"<token>"}` | 204 | 204 | PASS |
| 20 | Refresh after successful logout | `POST /api/auth/refresh` with now-revoked token | 401 | 401 | PASS |

---

## Summary

| Status | Count |
|--------|-------|
| PASS   | 20    |
| FAIL   | 0     |
| SKIP   | 1 (DELETE not-own quote — only 1 user in DB) |

All deployed endpoints respond correctly end-to-end.
