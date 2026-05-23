# Smoke Test Commands

**Base URL:** `https://ca-api-nb3bgcnwnlpwe.lemoncliff-d4727121.southeastasia.azurecontainerapps.io`

Set the base URL once:
```powershell
$BASE = "https://ca-api-nb3bgcnwnlpwe.lemoncliff-d4727121.southeastasia.azurecontainerapps.io"
```

---

## Health

```powershell
Invoke-WebRequest -Uri "$BASE/health" -SkipHttpErrorCheck
```

---

## Auth — Login

```powershell
# Valid credentials
Invoke-WebRequest -Uri "$BASE/api/auth/login" -Method POST `
    -ContentType "application/json" `
    -Body '{"email":"test@example.com","password":"password123"}' `
    -SkipHttpErrorCheck

# Wrong credentials
Invoke-WebRequest -Uri "$BASE/api/auth/login" -Method POST `
    -ContentType "application/json" `
    -Body '{"email":"nobody@x.com","password":"wrong"}' `
    -SkipHttpErrorCheck
```

Capture tokens for subsequent requests:
```powershell
$loginR  = Invoke-WebRequest -Uri "$BASE/api/auth/login" -Method POST `
               -ContentType "application/json" `
               -Body '{"email":"test@example.com","password":"password123"}' `
               -SkipHttpErrorCheck
$tokens      = $loginR.Content | ConvertFrom-Json
$accessToken = $tokens.access_token
$refreshToken = $tokens.refresh_token
$authHdr     = @{ Authorization = "Bearer $accessToken" }
```

---

## Quotes — Anonymous reads

```powershell
# List (page + size are required)
Invoke-WebRequest -Uri "$BASE/api/quotes/?page=1&size=10" -SkipHttpErrorCheck

# List without params — expect 400
Invoke-WebRequest -Uri "$BASE/api/quotes/" -SkipHttpErrorCheck

# Get by ID
Invoke-WebRequest -Uri "$BASE/api/quotes/3" -SkipHttpErrorCheck

# Non-existent ID — expect 404
Invoke-WebRequest -Uri "$BASE/api/quotes/99999" -SkipHttpErrorCheck
```

---

## Quotes — Authenticated writes

```powershell
# Create without auth — expect 401
Invoke-WebRequest -Uri "$BASE/api/quotes/" -Method POST `
    -ContentType "application/json" `
    -Body '{"author":"Marcus Aurelius","text":"The impediment to action advances action."}' `
    -SkipHttpErrorCheck

# Create with auth — expect 201
Invoke-WebRequest -Uri "$BASE/api/quotes/" -Method POST `
    -ContentType "application/json" `
    -Body '{"author":"Marcus Aurelius","text":"The impediment to action advances action."}' `
    -Headers $authHdr `
    -SkipHttpErrorCheck

# Create with empty fields — expect 400 ValidationProblem
Invoke-WebRequest -Uri "$BASE/api/quotes/" -Method POST `
    -ContentType "application/json" `
    -Body '{"author":"","text":""}' `
    -Headers $authHdr `
    -SkipHttpErrorCheck
```

---

## Quotes — Delete

```powershell
# Assumes $quoteId is the ID of a quote owned by the logged-in user

# Delete own quote — expect 204
Invoke-WebRequest -Uri "$BASE/api/quotes/$quoteId" -Method DELETE `
    -Headers $authHdr -SkipHttpErrorCheck

# Delete already-deleted quote — expect 404
Invoke-WebRequest -Uri "$BASE/api/quotes/$quoteId" -Method DELETE `
    -Headers $authHdr -SkipHttpErrorCheck

# Delete non-existent — expect 404
Invoke-WebRequest -Uri "$BASE/api/quotes/99999" -Method DELETE `
    -Headers $authHdr -SkipHttpErrorCheck

# Delete without auth — expect 401
Invoke-WebRequest -Uri "$BASE/api/quotes/1" -Method DELETE -SkipHttpErrorCheck
```

---

## Auth — Refresh token

> The request body field is **`refresh_token`** (snake_case).

```powershell
$refreshBody = ([PSCustomObject]@{ refresh_token = $refreshToken } | ConvertTo-Json -Compress)

# Valid refresh — expect 200 + new tokens
Invoke-WebRequest -Uri "$BASE/api/auth/refresh" -Method POST `
    -ContentType "application/json" `
    -Body $refreshBody `
    -SkipHttpErrorCheck

# Reuse rotated token — expect 401
Invoke-WebRequest -Uri "$BASE/api/auth/refresh" -Method POST `
    -ContentType "application/json" `
    -Body $refreshBody `
    -SkipHttpErrorCheck

# Invalid token — expect 401
Invoke-WebRequest -Uri "$BASE/api/auth/refresh" -Method POST `
    -ContentType "application/json" `
    -Body '{"refresh_token":"totally-fake-token"}' `
    -SkipHttpErrorCheck
```

---

## Auth — Logout

> The request body field is also **`refresh_token`** (snake_case).

```powershell
$logoutBody = ([PSCustomObject]@{ refresh_token = $newRefresh } | ConvertTo-Json -Compress)

# Logout without auth — expect 401
Invoke-WebRequest -Uri "$BASE/api/auth/logout" -Method POST `
    -ContentType "application/json" `
    -Body $logoutBody `
    -SkipHttpErrorCheck

# Logout with auth — expect 204
Invoke-WebRequest -Uri "$BASE/api/auth/logout" -Method POST `
    -ContentType "application/json" `
    -Body $logoutBody `
    -Headers $newAuthHdr `
    -SkipHttpErrorCheck

# Refresh after logout (token revoked) — expect 401
Invoke-WebRequest -Uri "$BASE/api/auth/refresh" -Method POST `
    -ContentType "application/json" `
    -Body $logoutBody `
    -SkipHttpErrorCheck
```
