# Thinkbridge Thinkschool Submissions - Aryan Bhalerao
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
    - QuotesApi - Main branch code with reviewed changes pushed from Claude branch 
    - WHY.ms
  - claude_workspace branch
    - QuotesApi - Unreviewed changes made entirely by Claude
