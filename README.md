# Thinkbridge Thinkschool Submissions - Aryan Bhalerao

NOTE: All pieces are commited on time. Only the day and projects directory's modified time has changed while removing /bin and /obj files for faster CI runs. The modified time for the content remains the same.

## Week 1
### Day 1
#### Piece 1 - Tools Check
- Completed setup and installation of latest versions of git, Microsoft .NET, Node, Angular, VS Code, Github Copilot and Claude
- Verified using --version command.
- Contents:
  - terminalOutput.txt

#### Piece 2 - Hello in Two Languages
- Implemented simple programs displaying "hello, Aryan" in C# and typescript.
- Contents:
  - hello-cs
  - hello-ts
  - readme.md

#### Piece 3 — ASP.NET Core 10 Minimal API
- Implmented minimal QuotesAPI in ASP .NET 10.
- Contents:
  - QuotesApi
  - curlResults.txt

#### Piece 4 — Refactor a god-method controller
- Generated a badly refactored project using claude.
- Successfully refactored the project.
- Successfully wrote tests which returned passed.
- Contents:
  - RefactoringExcercise
    - OrignalCode
    - src/OrdersApi
    - tests/OrderApi
  - Prompt.txt
  - REFACTOR_NOTES.md
  - badge.md
    - [![.NET Tests](https://github.com/thinkbridge-thinkschool/thinkschool-AryanBhalerao/actions/workflows/dotnet-tests.yml/badge.svg)](https://github.com/thinkbridge-thinkschool/thinkschool-AryanBhalerao/actions/workflows/dotnet-tests.yml)
  - output.md

#### Piece 5 - Real AI Assisted Work, Not Toys
- Successfully tested and analyzed the both for Claude and Copilot.
- Compared Claude and Copilots. Analyzed merits and demerits of Claude and Copilot.
- Contents:
  - main branch
    - OrderAPI
    - AI_REFLECTION.md
  - claude_workspace branch
    - OrderAPI
  - copilot_workspace branch
    - OrderAPI

#### Piece 7 - Build a Real Aggregate
- Successfully added a aggregate Collection and endpoints to QuotesApi.
- Contents:
  - QuotesApi
  - curl.md

### Day 2

#### Piece 1 - Dependency injection at depth
- Added a IClock abstraction returns DateTimeOffset.UtcNow — singleton and injected it everywhere that used DateTime.UtcNow.
- Successfully wrote tests which returned passed.
- Contents:
  - OrderApi
  - OrderApi.Tests
  - outputs.md
  - DI.md
  - iclock.md
  - iclockprod.md
  - tests.md

#### Piece 2 - async/await with cancellation through layers
- Added a cancellation token parameter to every async method that takes I/O
- Modified token flow to support cancellation.
- Successfully wrote tests which returned passed.
- Contents:
  - QuotesApi
  - QuotesApi.Tests
  - outputs.md
  - cancellation-aware-service.md

#### Piece 3 - Test the domain layer
- Set up xUnit + Fluent Assertions Tests project to test listed cases for QuotesApi.
- Tests successfully returned pass.
- Contents:
  - QuotesApi
  - Tests.Domain
  - outputs.md
  - tests.md

#### Piece 4 - AI-assisted refactor: anemic to rich
- Converted QuotesApi's quotes entity from anemic to rich using Claude.
- Reviewed code, made edits where necessary and merged with main.
- Contents:
  - main branch
    - QuotesApi
    - output.md
    - WHY.md
  - rich branch
    - QuotesApi
    - output.md

#### Piece 6 - Implement JWT auth (your own issuer)
- In the Quotes API, added a minimal POST /api/auth/login endpoint that takes {email, password} and returns {access_token, refresh_token, expires_in}.
- Contents:
  - QuotesApi
  - curl.md
  - endpoint.md

#### Piece 7 - Refresh tokens with rotation
- In the Quotes API, added refresh tokens with rotation.
- Contents:
  - QuotesApi
  - QuotesApi.Tests
  - endpoint.md
  - test.md

### Day 3

#### Piece 1 - Bring your API behind Entra ID
- Tested QuotesApi endpoint access using Microsoft Entra ID bearer token.
- Verified authenticated POST request returns `201 Created`.
- Contents:
  - QuotesApi
  - curl.md

#### Piece 2 - Layer authorization with policies
- Added policy-based authorization for scope claim (`quotes.write`) and quote ownership.
- Verified `403 Forbidden` for missing scope and non-owner delete attempts.
- Contents:
  - QuotesApi
  - policies.md
  - tests.md

#### Piece 3 - CI hardening for auth paths
- Added CI coverage for key auth and token lifecycle scenarios.
- Included checks for anonymous access, insufficient permissions, valid policy access, token expiry, and revoked refresh chain.
- Contents:
  - QuotesApi
  - QuotesApi.Tests
  - CIrun.md

#### Piece 5 - Expand unit test depth
- Added comprehensive unit test coverage for domain models and authorization logic.
- Verified all listed tests pass in local run and CI.
- Contents:
  - QuotesApi
  - Quotes.Tests.Unit
  - SampleTests.md
  - testsOutput.md
  - CIRun.md

#### Piece 6 - Integration test infrastructure with WebApplicationFactory
- Implemented reusable integration test setup using `WebApplicationFactory` and SQLite test database isolation.
- Added representative happy-path and error-path integration tests.
- Contents:
  - QuotesApi
  - Quotes.Tests.Integration
  - factory.md
  - tests.md
  - TestsOutput.md

#### Piece 7 - Testcontainers + SQL Server integration in CI
- Added Testcontainers-based SQL Server fixture for realistic integration test execution.
- Configured GitHub Actions workflow to run integration suite with SQL Server 2022 container.
- Contents:
  - QuotesApi
  - Quotes.Tests.Integration
  - actions.md
  - TestContainers.md
  - TestsList.md
  - CIRun.md

### Day 4

#### Piece 1 - Status Check Requirement
- Configured the Day 3 Piece 7 CI workflow as a required status check for merging to main.
- Verified a passing CI run before merge.
- Contents:
  - QuotesApi
  - QuotesApi.Tests
  - Quotes.Tests.Integration
  - Quotes.Tests.Unit
  - CIRun.md
  - screenshot.md

#### Piece 2 - Code Coverage Analysis
- Measured and analysed code coverage across integration and unit test suites using `dotnet test --collect:"XPlat Code Coverage"`.
- Combined coverage: ~99% line coverage, >80% branch coverage across 65 tests (23 integration + 42 unit).
- Most surprising gap: `Quote.Create` static factory sat at 0% in integration tests because endpoints bypass it entirely using object initializers; treated as a design smell — validation logic was duplicated across two abstractions with divergent rules.
- Contents:
  - QuotesApi
  - QuotesApi.Tests
  - Quotes.Tests.Integration
  - Quotes.Tests.Unit
  - coverage.runsettings
  - coverageReport.md
  - Exercise.md

#### Piece 4 - Structured Logging with Serilog
- Added Serilog with a two-stage bootstrap so startup exceptions are captured before configuration is read.
- Enabled request correlation: `LogContext.PushProperty("TraceId", ctx.TraceIdentifier)` links all log events in a request by a shared TraceId.
- Used structured (indexed key-value) logging throughout; EF Core SQL logging enabled in Development via `appsettings.Development.json`.
- Contents:
  - QuotesApi
  - QuotesApi.Tests
  - Quotes.Tests.Integration
  - Quotes.Tests.Unit
  - SerilogSetup.md
  - LogOutput.md

#### Piece 5 - Distributed Tracing with OpenTelemetry + Jaeger
- Instrumented QuotesApi with OpenTelemetry tracing for ASP.NET Core, EF Core, and HttpClient.
- Added a custom `ActivitySource` (`QuotesApi.Quotes`) with a hand-crafted `authorize-delete-quote` span carrying `quote.id`, `user.id`, `quote.owner_id`, and `authorized` tags.
- Wired log–trace correlation: the same TraceId pushed by Serilog middleware appears on every Jaeger span.
- Contents:
  - QuotesApi
  - QuotesApi.Tests
  - Quotes.Tests.Integration
  - Quotes.Tests.Unit
  - OTel.md
  - jaeger.md

#### Piece 6 - Azure Application Insights + KQL Monitoring
- Deployed QuotesApi to Azure App Service; connected Application Insights (workspace-based) via OpenTelemetry's `UseAzureMonitor()`.
- Stored the App Insights connection string as a Key Vault secret (`ApplicationInsights--ConnectionString`) and retrieved it at startup via `DefaultAzureCredential` / managed identity.
- Created a KQL query for the top 10 slowest requests and an alert rule on `requests/duration` > 500 ms for `POST /api/quotes`.
- Contents:
  - QuotesApi
  - QuotesApi.Tests
  - Quotes.Tests.Integration
  - Quotes.Tests.Unit
  - InsightsSetup.md
  - cloudSetup.md
  - KQLquery.md
  - KQIOutput.md

#### Piece 7 - Configuration Management
- Introduced a typed `JwtOptions` record bound via `IOptions<JwtOptions>` with startup validation that throws if `Jwt:SigningKey` is absent.
- Layered configuration: defaults in `appsettings.json`, signing key via `dotnet user-secrets` locally, and Key Vault reference in production via `KeyVault__Uri`.
- Contents:
  - QuotesApi
  - QuotesApi.Tests
  - Quotes.Tests.Integration
  - Quotes.Tests.Unit
  - Configurations.md

### Day 5

#### Piece 1 - Performance Analysis with Jaeger
- Identified an N+1 query pattern in `GET /api/quotes` using Jaeger trace spans — endpoint fetched all quotes then fired a separate DB query per owner plus a 150 ms simulated sleep per iteration.
- Fixed by replacing the N+1 loop with the existing `GetPagedAsync` single query, dropping response time from ~1.27 s to ~7 ms.
- Contents:
  - QuotesApi
  - Jaegar.md
  - KQLQuery.md
  - Note.md

#### Piece 2 - Containerize the API
- Built a Docker image for QuotesApi using `dotnet publish` container support (`ContainerImageName`, `ContainerImageTag`, `ContainerBaseImage` set in csproj).
- Ran the container locally with `docker run`, injecting `Jwt__SigningKey` and `ConnectionStrings__Default` as environment variables.
- Verified `GET /health` returned `200 Healthy` from the running container.
- Contents:
  - QuotesApi
  - csproj.md
  - dockerOutput.md
  - curl.md

#### Piece 3 - Azure Container Apps Environment Setup
- Created an Azure Container Apps Environment via `az CLI` in `southeastasia` region (required by subscription policy — `centralindia` blocked).
- Contents:
  - QuotesApi
  - AzureSetup.md
  - Output.md

#### Piece 4 - Deploy to Azure Container Apps with azd
- Deployed QuotesApi to Azure Container Apps using Azure Developer CLI (`azd up`) with an `azure.yaml` service definition.
- `Jwt__SigningKey` injected as a Container Apps secret; Key Vault skipped for the container deployment; SQLite DB is ephemeral (re-seeded on each start).
- Live URL: `https://ca-api-nb3bgcnwnlpwe.lemoncliff-d4727121.southeastasia.azurecontainerapps.io`
- Contents:
  - QuotesApi
  - AzureSetup.md
  - curl.md

#### Piece 5 - App Insights KQL Monitoring on Production
- Queried Application Insights for request metrics (count, p50, p99) on the deployed app using KQL.
- Most surprising finding: `GET /health` had a 575× p50/p99 gap (0.48 ms vs 279 ms) — the no-op health route had the worst tail latency because the single-replica Container Apps instance can be parked in a low-priority CPU slot between requests.
- Contents:
  - QuotesApi
  - Observations.md
  - KQLResults.md

#### Piece 6 - Polly Resilience for Entra ID HttpClient
- Added retry (exponential backoff with jitter, up to 3 retries) and circuit breaker (50% failure ratio over 30 s, 30 s break) to the Entra ID `HttpClient` using `Microsoft.Extensions.Http.Resilience`.
- Wired the resilient handler into `JwtBearerOptions.BackchannelHttpHandler` via `IHttpMessageHandlerFactory`.
- Wrote unit tests verifying retry count, structured log output per attempt, and that exhausted retries surface the last bad response.
- Contents:
  - QuotesApi
  - Quotes.Tests.Unit
  - config.md
  - tests.md

#### Piece 7 - End-to-End Smoke Tests
- Ran 20 smoke tests against the deployed Azure Container Apps instance covering health, auth (login/refresh/logout), anonymous reads, authenticated writes, and deletes.
- All 20 checks passed; 1 skipped (cross-user delete — only one user exists in the deployed DB).
- Documented 10 fragility notes including: 500 on wrong `refresh_token` field name, inconsistent request/response field casing, no pagination defaults, ephemeral SQLite DB, and missing rate limiting on auth endpoints.
- Contents:
  - ResultsSummary.md
  - SmokeTests.md
  - Outputs.md
  - FragilityNotes.md

### Day 7

#### Piece 1 - CTE Query
- Wrote a CTE query returning each author with their quote count and most-recent quote.
- Used two CTEs (`AuthorStats` for `COUNT`/`MAX`, `RankedQuotes` for `ROW_NUMBER`) joined cleanly to avoid re-executing correlated subqueries per row.
- Contents:
  - DBSetup.sql
  - query.sql
  - FullResult.csv
  - Solution.md

#### Piece 2 - Window Functions Query
- Wrote a window function query returning per-author quote timeline with running count and gap in days since previous quote.
- Used `ROW_NUMBER`, `RANK`, `LAG`, `LEAD`, and `SUM ... ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW` in a single pass.
- Contents:
  - DBSetup.sql
  - query.sql
  - FullResult.csv
  - Solution.md

#### Piece 3 - Set Operations
- Wrote three queries using EXCEPT, INTERSECT, and UNION set operators.
- Query 1 (EXCEPT): authors with quotes but no tags. Query 2 (INTERSECT): authors appearing in both 'classic' and 'modern' categories. Query 3 (UNION): combined distinct tag list across both categories.
- Contents:
  - DBSetup.sql
  - query.sql
  - Solution.md
