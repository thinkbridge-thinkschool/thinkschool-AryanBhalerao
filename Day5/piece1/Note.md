Slow Span Commit Note:
NOTE: The GET /api/quotes endpoint first fetches all 8 quotes in a single query, then loops over each one and fires a separate database query to load its owner — that's the N+1 pattern. On top of that, each iteration also sleeps for 150 ms to simulate network latency. So instead of one DB round-trip, you get 8 sequential ones, each with an artificial wait.

Slow Span Fix Commit Note:
NOTE: Replaced the N+1 loop with the single GetPagedAsync query that was already there. Instead of fetching quotes first and then hitting the database again for each owner individually. The whole request now maps to one DB span in Jaeger, dropping response time from ~1.27 s to ~7 ms.