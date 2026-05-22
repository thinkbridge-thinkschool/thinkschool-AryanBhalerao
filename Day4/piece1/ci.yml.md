# ci.yml

```yaml
name: CI – Day4/Piece1

on:
  push:
    branches: ["**"]
  pull_request:
    branches: [main]

jobs:
  build-and-test:
    name: Build & Test
    runs-on: ubuntu-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"

      - name: Restore
        run: dotnet restore Day4/Piece1/QuotesApi.Tests/QuotesApi.Tests.csproj

      - name: Build
        run: dotnet build Day4/Piece1/QuotesApi.Tests/QuotesApi.Tests.csproj --no-restore

      - name: Test
        run: >
          dotnet test Day4/Piece1/QuotesApi.Tests/QuotesApi.Tests.csproj
          --no-build
          --logger "trx;LogFileName=test-results.trx"
          --collect:"XPlat Code Coverage"
          --results-directory ./TestResults

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: test-results
          path: ./TestResults/**/*.trx

      - name: Upload coverage
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: coverage-report
          path: ./TestResults/**/coverage.cobertura.xml

      - name: Check coverage threshold (≥ 70 %)
        run: |
          python3 - <<'EOF'
          import glob, xml.etree.ElementTree as ET, sys

          files = glob.glob("TestResults/**/coverage.cobertura.xml", recursive=True)
          if not files:
              print("No coverage.cobertura.xml found — did the test step produce coverage?")
              sys.exit(1)

          tree = ET.parse(files[0])
          root = tree.getroot()
          line_rate = float(root.attrib.get("line-rate", 0))
          pct = round(line_rate * 100, 1)
          print(f"Line coverage: {pct}%")

          if pct < 70:
              print(f"ERROR: {pct}% is below the 70% threshold.")
              sys.exit(1)

          print("Coverage check passed.")
          EOF
```

## Design decisions

| Choice | Reason |
|--------|--------|
| `push: branches: ["**"]` | CI runs on every branch so failures are caught before a PR is even opened. |
| `pull_request: branches: [main]` | Every PR targeting main must pass before merge (enforced via branch protection). |
| `--no-restore` / `--no-build` | Each step reuses the previous step's output — avoids redundant work. |
| `--collect:"XPlat Code Coverage"` | Cross-platform Cobertura XML that coverlet writes alongside the TRX file. |
| `if: always()` on upload steps | Artifacts are uploaded even when tests fail, so the TRX and coverage XML are still available for inspection. |
| Python coverage check | Pure stdlib (`xml.etree.ElementTree`) — no extra tool installation needed. Exits 1 when line rate < 70 %, which fails the job. |

## Branch protection setup (do this once in GitHub)

1. Go to **Settings → Branches → Add branch protection rule**.
2. Branch name pattern: `main`
3. Enable **"Require status checks to pass before merging"**.
4. Search for and add the status check: **`Build & Test`** (the `name:` of the job in ci.yml).
5. Enable **"Require branches to be up to date before merging"**.
6. Save.

PRs to main will now be blocked until the `CI – Day4/Piece1` workflow reports green.
