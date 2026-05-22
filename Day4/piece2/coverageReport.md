# Coverage Report

Run command:
```
dotnet test --settings coverage.runsettings --collect:"XPlat Code Coverage"
```

Migrations and compiler-generated code are excluded via `coverage.runsettings`.

---

## Integration Tests — `QuotesApi.Tests`

**Line rate: 95.6% (304 / 318 lines) | Branch rate: 44.1%**

| Class | Line % | Branch % |
|---|---|---|
| `AuthEndpoints` | 100% | 100% |
| `QuoteEndpoints` | 100% | 100% |
| `GlobalExceptionHandler` | 100% | 100% |
| `InfrastructureExtensions` | 100% | 50% |
| `OwnQuoteHandler` | 100% | 100% |
| `QuoteRepository` | 100% | 100% |
| `QuoteValidator` | 100% | 100% |
| `RefreshTokenRepository` | 100% | 100% |
| `SystemClock` | 100% | 100% |
| `TokenHasher` | 100% | 100% |
| `Quote` (model) | **0%** | 0% |

Note: `InfrastructureExtensions` branch rate is 50% because the Azure AD issuer-forwarding selector (`ForwardDefaultSelector` — "if issuer starts with `https://login.microsoftonline.com/`") is never taken in tests. `TestAuthHandler` bypasses JWT routing entirely, so hitting the real Entra ID branch would require a live tenant. This is acceptable dead-test territory.

---

## Unit Tests — `Quotes.Tests.Unit`

**Line rate: 19.2% (61 / 318 lines) | Branch rate: 76.5%**

Unit tests target the classes that are hard to exercise through HTTP:

| Class | Line % | Branch % |
|---|---|---|
| `GlobalExceptionHandler` | 100% | 100% |
| `OwnQuoteHandler` | 100% | 100% |
| `QuoteRepository` | 100% | 100% |
| `QuoteValidator` | 100% | 100% |
| `RefreshTokenRepository` | 100% | 100% |
| `SystemClock` | 100% | 100% |
| `TokenHasher` | 100% | 100% |
| `Quote` (model — `Quote.Create`) | **100%** | 100% |

---

## Combined Coverage

The 14 lines at 0% in the integration run (`Quote.Create` static factory) are covered 100% by unit tests. Union of both runs:

**~99% line coverage | >80% branch coverage**


---

## Test counts

| Project | Tests |
|---|---|
| `QuotesApi.Tests` (integration) | 23 |
| `Quotes.Tests.Unit` (unit) | 42 |
| **Total** | **65** |

All 65 pass.
