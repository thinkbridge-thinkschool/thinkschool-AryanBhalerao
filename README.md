# Thinkbridge Thinkschool Submissions - Aryan Bhalerao

NOTE: All pieces are commited on time. Only the day and projects directory's modified time has changed while removing /bin and /obj files for faster CI runs. The modified time for the content remains the same.

## Week 1
### Day 1
#### Piece 1 - Tools Check
- Completed setup and installation of latest versions of git, Microsoft .NET, Node, Angular, VS Code, Github Copilot and Claude
- Verified using --version command.
- Contents:
  - terminalOutput.txt - contains commands and outputs of --version commands
  - output.png - snapshot of the same

#### Piece 2 - Hello in Two Languages
- Implemented simple programs displaying "hello, Aryan" in C# and typescript.
- Contents:
  - hello-cs - C# program 
  - hello-ts - typescript program
  - readme.md - Output

#### Piece 3 — ASP.NET Core 10 Minimal API
- Implmented minimal QuotesAPI in ASP .NET 10.
- Contents:
  - QuotesApi - ASP .NET Web project
  - curlResults.txt - Endpoint testing using curl. Commands and output.

#### Piece 4 — Refactor a god-method controller
- Generated a badly refactored project using claude.
- Successfully refactored the project.
- Successfully wrote tests which returned passed.
- Contents:
  - RefactoringExcercise
    - OrignalCode - orignal generated code with bad refactoring
    - src/OrdersApi - correct code
    - tests/OrderApi - tests for CI Badge
  - Prompt.txt - prompt given to generate Orignal Code
  - REFACTOR_NOTES.md
  - badge.md
    - [![.NET Tests](https://github.com/thinkbridge-thinkschool/thinkschool-AryanBhalerao/actions/workflows/dotnet-tests.yml/badge.svg)](https://github.com/thinkbridge-thinkschool/thinkschool-AryanBhalerao/actions/workflows/dotnet-tests.yml)
  - output.md

#### Piece 5 - Real AI Assisted Work, Not Toys
- Successfully tested and analyzed the both for Claude and Copilot.
- Compared Claude and Copilots. Analyzed merits and demerits of Claude and Copilot.
- Contents:
  - main branch
    - OrderAPI - Main branch code with reviewed changes pushed from both branches
    - AI_REFLECTION.md
  - claude_workspace branch
    - OrderAPI - Modified with claude
  - copilot_workspace branch
    - OrderAPI - Modified with copilot

#### Piece 7 - Build a Real Aggregate
- Successfully added a aggregate Collection and endpoints to QuotesApi.
- Contents:
  - QuotesApi - Modified QuotesApi with collection
  - curl.md - curl test showing 400 Problem Details

### Day 2

#### Piece 1 - Dependency injection at depth
- Added a IClock abstraction returns DateTimeOffset.UtcNow — singleton and injected it everywhere that used DateTime.UtcNow.
- Successfully wrote tests which returned passed.
- Contents:
  - OrderApi - Modified OrderApi with dependency injection at depth.
  - OrderApi.Tests - Tests to verify the listed cases
  - outputs.md - Output of build and tests.
  - DI.md 
  - iclock.md
  - iclockprod.md
  - tests.md

#### Piece 2 - async/await with cancellation through layers
- Added a cancellation token parameter to every async method that takes I/O 
- Modified token flow to support cancellation.
- Successfully wrote tests which returned passed.
- Contents:
  - QuotesApi - Modified QuotesApi with cancellation through layers
  - QuotesApi.Tests - Tests to verify the listed cases
  - outputs.md - Output of build and tests.
  - cancellation-aware-service.md

#### Piece 3 - Test the domain layer
- Set up xUnit + Fluent Assertions Tests project to test listed cases for QuotesApi.
- Tests successfully returned pass.
- Contents:
  - QuotesApi - Quotes project from Day 2 Piece 2 to run tests on. 
  - Tests.Domain - Tests to very given cases
  - outputs.md
  - tests.md

#### Piece 4 - AI-assisted refactor: anemic to rich
- Converted QuotesApi's quotes entity from anemic to rich using Claude.
- Reviewed code, made edits where necessary and merged with main.
- Contents:
  - main branch
    - QuotesApi - Main branch code with reviewed changes merged from rich branch 
    - output.md
    - WHY.md
  - rich branch
    - QuotesApi - Refactored with rich Quote entity using Claude
    - output.md

#### Piece 6 - Implement JWT auth (your own issuer)
- In the Quotes API, added a minimal POST /api/auth/login endpoint that takes {email, password} and returns {access_token, refresh_token, expires_in}.
- Contents:
  - QuotesApi
  - curl.md - curl commands and outputs to test given endpoints
  - endpoint.md - code for login endpoint

#### Piece 7 - Refresh tokens with rotation
- In the Quotes API, added refresh tokens with rotation.
- Contents:
  - QuotesApi
  - QuotesApi.Tests
  - endpoint.md - code for refresh endpoint
  - test.md - code and output for tests for given cases

### Day 3

#### Piece 1 - Bring your API behind Entra ID
- Tested QuotesApi endpoint access using Microsoft Entra ID bearer token.
- Verified authenticated POST request returns `201 Created`.
- Contents:
  - QuotesApi - API project configured for token-based access.
  - curl.md - token retrieval, curl command, and successful response output.

#### Piece 2 - Layer authorization with policies
- Added policy-based authorization for scope claim (`quotes.write`) and quote ownership.
- Verified `403 Forbidden` for missing scope and non-owner delete attempts.
- Contents:
  - QuotesApi - policy configuration and authorization handler integration.
  - policies.md - policy setup and relevant code snippets.
  - tests.md - policy test cases and test output.

#### Piece 3 - CI hardening for auth paths
- Added CI coverage for key auth and token lifecycle scenarios.
- Included checks for anonymous access, insufficient permissions, valid policy access, token expiry, and revoked refresh chain.
- Contents:
  - QuotesApi
  - QuotesApi.Tests
  - CIrun.md - workflow badge and covered scenarios.

#### Piece 5 - Expand unit test depth
- Added comprehensive unit test coverage for domain models and authorization logic.
- Verified all listed tests pass in local run and CI.
- Contents:
  - QuotesApi
  - Quotes.Tests.Unit
  - SampleTests.md - representative tests.
  - testsOutput.md - local test run output (`44/44` passed).
  - CIRun.md - [![Piece 5 Unit Tests](https://github.com/thinkbridge-thinkschool/thinkschool-AryanBhalerao/actions/workflows/day3piece5ci.yml/badge.svg)](https://github.com/thinkbridge-thinkschool/thinkschool-AryanBhalerao/actions/workflows/day3piece5ci.yml) 

#### Piece 6 - Integration test infrastructure with WebApplicationFactory
- Implemented reusable integration test setup using `WebApplicationFactory` and SQLite test database isolation.
- Added representative happy-path and error-path integration tests.
- Contents:
  - QuotesApi
  - Quotes.Tests.Integration
  - factory.md - test factory implementation.
  - tests.md - representative integration tests.
  - TestsOutput.md - integration test run output (`25/25` passed).

#### Piece 7 - Testcontainers + SQL Server integration in CI
- Added Testcontainers-based SQL Server fixture for realistic integration test execution.
- Configured GitHub Actions workflow to run integration suite with SQL Server 2022 container.
- Contents:
  - QuotesApi
  - Quotes.Tests.Integration
  - actions.md - CI workflow for container-backed integration tests.
  - TestContainers.md - fixture and factory setup details.
  - TestsList.md - list of integration test cases.
  - CIRun.md - [![Piece 7 Integration Tests](https://github.com/thinkbridge-thinkschool/thinkschool-AryanBhalerao/actions/workflows/day3piece7ci.yml/badge.svg)](https://github.com/thinkbridge-thinkschool/thinkschool-AryanBhalerao/actions/workflows/day3piece7ci.yml)  

### Day 4

#### Piece 1 - Status Check Requirement
- Configured the Day 3 Piece 7 CI workflow as a required status check for merging to main.
- Verified a passing CI run before merge.
- Contents:
  - QuotesApi
  - QuotesApi.Tests
  - Quotes.Tests.Integration
  - Quotes.Tests.Unit
  - CIRun.md - [![Status Check Requirement](https://github.com/thinkbridge-thinkschool/thinkschool-AryanBhalerao/actions/workflows/day3piece7ci.yml/badge.svg)](https://github.com/thinkbridge-thinkschool/thinkschool-AryanBhalerao/actions/workflows/day3piece7ci.yml)
  - screenshot.md - merge screenshot after status check validation.

#### Piece 2 - Code Coverage Analysis
- Measured and analysed code coverage across integration and unit test suites using `dotnet test --collect:"XPlat Code Coverage"`.
- Combined coverage: ~99% line coverage, >80% branch coverage across 65 tests (23 integration + 42 unit).
- Most surprising gap: `Quote.Create` static factory sat at 0% in integration tests because endpoints bypass it entirely using object initializers; treated as a design smell — validation logic was duplicated across two abstractions with divergent rules.
- Contents:
  - QuotesApi
  - QuotesApi.Tests
  - Quotes.Tests.Integration
  - Quotes.Tests.Unit
  - coverage.runsettings - excludes migrations and compiler-generated code.
  - coverageReport.md - per-class line and branch rates for both test projects.
  - Exercise.md - analysis of the most surprising uncovered branch.

#### Piece 4 - Structured Logging with Serilog
- Added Serilog with a two-stage bootstrap so startup exceptions are captured before configuration is read.
- Enabled request correlation: `LogContext.PushProperty("TraceId", ctx.TraceIdentifier)` links all log events in a request by a shared TraceId.
- Used structured (indexed key-value) logging throughout; EF Core SQL logging enabled in Development via `appsettings.Development.json`.
- Contents:
  - QuotesApi
  - QuotesApi.Tests
  - Quotes.Tests.Integration
  - Quotes.Tests.Unit
  - SerilogSetup.md - package list, Program.cs bootstrap, appsettings Serilog section, structured vs interpolated logging rules.
  - LogOutput.md - sample correlated log output showing TraceId across EF Core, application, and middleware lines.

#### Piece 5 - Distributed Tracing with OpenTelemetry + Jaeger
- Instrumented QuotesApi with OpenTelemetry tracing for ASP.NET Core, EF Core, and HttpClient.
- Added a custom `ActivitySource` (`QuotesApi.Quotes`) with a hand-crafted `authorize-delete-quote` span carrying `quote.id`, `user.id`, `quote.owner_id`, and `authorized` tags.
- Wired log–trace correlation: the same TraceId pushed by Serilog middleware appears on every Jaeger span.
- Contents:
  - QuotesApi
  - QuotesApi.Tests
  - Quotes.Tests.Integration
  - Quotes.Tests.Unit
  - OTel.md - package list, tracing configuration, custom span code.
  - jaeger.md - Jaeger setup notes.
  - jaeger.png / jaegerSpans.png - Jaeger UI screenshots.

#### Piece 6 - Azure Application Insights + KQL Monitoring
- Deployed QuotesApi to Azure App Service; connected Application Insights (workspace-based) via OpenTelemetry's `UseAzureMonitor()`.
- Stored the App Insights connection string as a Key Vault secret (`ApplicationInsights--ConnectionString`) and retrieved it at startup via `DefaultAzureCredential` / managed identity.
- Created a KQL query for the top 10 slowest requests and an alert rule on `requests/duration` > 500 ms for `POST /api/quotes`.
- Contents:
  - QuotesApi
  - QuotesApi.Tests
  - Quotes.Tests.Integration
  - Quotes.Tests.Unit
  - InsightsSetup.md - package references and OpenTelemetry wiring in Program.cs / InfrastructureExtensions.cs.
  - cloudSetup.md - Azure portal steps for App Insights, Key Vault, RBAC access, App Service config, and alert rule.
  - KQLquery.md - KQL query used to surface slow requests.
  - KQIOutput.md / KQIOutput.png - query results screenshot.

#### Piece 7 - Configuration Management
- Introduced a typed `JwtOptions` record bound via `IOptions<JwtOptions>` with startup validation that throws if `Jwt:SigningKey` is absent.
- Layered configuration: defaults in `appsettings.json`, signing key via `dotnet user-secrets` locally, and Key Vault reference in production via `KeyVault__Uri`.
- Contents:
  - QuotesApi
  - QuotesApi.Tests
  - Quotes.Tests.Integration
  - Quotes.Tests.Unit
  - Configurations.md - JwtOptions record, appsettings.json excerpt, DI registration with startup validation, and injection example in AuthEndpoints.
