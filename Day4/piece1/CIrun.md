# Day4/Piece1 – CI Status

[![CI – Day4/Piece1](https://github.com/aryanbhalerao/thinkschool-AryanBhalerao/actions/workflows/ci.yml/badge.svg)](https://github.com/aryanbhalerao/thinkschool-AryanBhalerao/actions/workflows/ci.yml)

> Replace `<owner>` with your GitHub username to make the badge live.

---

## Test suite

19 integration tests covering the full QuotesApi surface.

### QuoteEndpointTests (10 tests)

| # | Test | Expected |
|---|------|----------|
| 1 | `GetQuotes_ReturnsOkWithList` | `GET /api/quotes?page=1&size=10` → 200 |
| 2 | `GetQuoteById_NotFound_Returns404` | `GET /api/quotes/99999` → 404 |
| 3 | `GetQuoteById_Existing_Returns200` | Create then GET by id → 200 |
| 4 | `PostQuote_Anonymous_Returns401` | No auth header → 401 |
| 5 | `PostQuote_AuthenticatedWithoutScope_Returns403` | Auth but no `scope=quotes.write` → 403 |
| 6 | `PostQuote_AuthenticatedWithScope_Returns201` | Auth + `scope=quotes.write` → 201 |
| 7 | `PostQuote_MissingAuthor_Returns400` | Empty author field → 400 |
| 8 | `DeleteQuote_Anonymous_Returns401` | No auth header → 401 |
| 9 | `DeleteQuote_ByNonOwner_Returns403` | Different `sub` claim → 403 |
| 10 | `DeleteQuote_ByOwner_Returns204` | Matching `sub` claim → 204 |
| 11 | `DeleteQuote_NonExistent_Returns404` | Quote id does not exist → 404 |

### AuthEndpointTests (8 tests)

| # | Test | Expected |
|---|------|----------|
| 1 | `Login_InvalidCredentials_Returns401` | Wrong password → 401 |
| 2 | `Login_UnknownUser_Returns401` | Unknown email → 401 |
| 3 | `Login_ValidCredentials_ReturnsTokens` | Valid login → 200 with `access_token` + `refresh_token` |
| 4 | `Refresh_InvalidToken_Returns401` | Garbage token → 401 |
| 5 | `Refresh_ValidToken_ReturnsNewTokens` | Valid refresh → 200 with new tokens |
| 6 | `Refresh_RevokedToken_Returns401` | Already-rotated token → 401 |
| 7 | `Refresh_ReuseDetected_RevokesEntireFamily` | Replay attack revokes whole chain → 401 for both old and new tokens |
| 8 | `Logout_ValidRefreshToken_Returns204` | Valid refresh token + auth → 204 |

---

## Coverage

The job fails if line coverage drops below **70 %**.  
The full Cobertura XML is uploaded as the `coverage-report` artifact on every run.

---

## Branch protection

The `Build & Test` status check is required on `main`.  
PRs cannot be merged until the check is green — red CI blocks the merge button.

Setup instructions: see [ci.yml.md](ci.yml.md#branch-protection-setup-do-this-once-in-github).
