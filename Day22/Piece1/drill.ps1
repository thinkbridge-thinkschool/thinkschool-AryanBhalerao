# Day 22 resilience drill: prove the circuit opens under sustained failure and recovers.
# Run with the API listening on http://localhost:5051.
$base = "http://localhost:5051"

function Invoke-Step($method, $url) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $resp = Invoke-WebRequest -Method $method -Uri $url -TimeoutSec 30 -SkipHttpErrorCheck
        $sw.Stop()
        "{0,-4} {1,-45} -> {2} in {3,5} ms" -f $method, ($url -replace [regex]::Escape($base), ''), $resp.StatusCode, $sw.ElapsedMilliseconds
    } catch {
        $sw.Stop()
        "{0,-4} {1,-45} -> EXCEPTION {2} in {3,5} ms" -f $method, ($url -replace [regex]::Escape($base), ''), $_.Exception.Message, $sw.ElapsedMilliseconds
    }
}

function Get-DownstreamHits {
    (Invoke-RestMethod "$base/downstream/fault").hits
}

Write-Output "=== Phase 0: baseline (downstream healthy) ==="
Invoke-RestMethod -Method Post "$base/downstream/fault/ok" | Out-Null
Invoke-Step GET "$base/api/authors/Seneca/profile"

Write-Output ""
Write-Output "=== Phase 1: downstream now failing (HTTP 500) ==="
Invoke-RestMethod -Method Post "$base/downstream/fault/fail" | Out-Null

Write-Output "-- POST is NOT retried (non-idempotent):"
Invoke-Step POST "$base/api/authors/Seneca/profile/refresh"
Write-Output ("   downstream hits after 1 POST: {0}  (1 = no retries)" -f (Get-DownstreamHits))

Write-Output "-- GET IS retried (idempotent, 1 try + 3 retries):"
Invoke-Step GET "$base/api/authors/Seneca/profile"
Write-Output ("   downstream hits now: {0}  (+4 = original try + 3 retries)" -f (Get-DownstreamHits))

Write-Output ""
Write-Output "=== Phase 2: sustained failure -> circuit should OPEN ==="
1..8 | ForEach-Object { Invoke-Step GET "$base/api/authors/Seneca/profile" }
Write-Output ("   downstream hits: {0}" -f (Get-DownstreamHits))

Write-Output ""
Write-Output "=== Phase 3: circuit open -> fast-fail, downstream NOT touched ==="
$hitsBefore = Get-DownstreamHits
1..5 | ForEach-Object { Invoke-Step GET "$base/api/authors/Seneca/profile" }
$hitsAfter = Get-DownstreamHits
Write-Output ("   downstream hits before/after 5 calls: {0} / {1}  (unchanged = breaker shielding)" -f $hitsBefore, $hitsAfter)

Write-Output ""
Write-Output "=== Phase 4: downstream recovers; wait out BreakDuration (10s) ==="
Invoke-RestMethod -Method Post "$base/downstream/fault/ok" | Out-Null
Start-Sleep -Seconds 11

Write-Output "-- first call after break: half-open probe should succeed and CLOSE the circuit"
Invoke-Step GET "$base/api/authors/Seneca/profile"
Invoke-Step GET "$base/api/authors/Seneca/profile"
Write-Output ""
Write-Output "Drill complete. Check the API console for retry / CIRCUIT OPENED / HALF-OPEN / CLOSED logs."
