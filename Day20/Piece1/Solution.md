# Day 20 · Piece 1 — The transactional outbox: a DB write and a queue publish that cannot diverge

The danger is **dual writes**: save the quote to the database, then publish an event to the broker. Those are two separate systems with no shared transaction, so there is a window where the DB commit succeeds and the publish fails (or the process dies) — the quote exists but no event was ever sent. The database and the queue have **diverged**, silently and permanently.

The **transactional outbox** removes that window. The event is written as an ordinary row in the *same database* and the *same transaction* as the domain change, so "quote exists" and "event is pending" commit together or not at all. Publishing is deferred to a separate **relay** that reads unsent rows, publishes them to the broker, and only then marks them sent. The relay is **at-least-once** (a crash can cause a re-publish), and an **idempotent consumer** keyed on the message id absorbs the duplicate — so no message is lost and none is processed twice.

This runs locally: EF Core over **SQLite** (real ACID transactions, no SQL Server install) for the DB, and Microsoft's official **Azure Service Bus emulator** in Docker (Day 19's broker) for the queue. Full code lives in [`OutboxDemo/`](OutboxDemo/); topic `quotes` with subscription `search-index` is declared in [config/servicebus-config.json](OutboxDemo/config/servicebus-config.json) and the broker comes up via [docker-compose.yml](OutboxDemo/docker-compose.yml).

## Setup

```powershell
cd OutboxDemo
docker compose up -d        # Azure Service Bus emulator + its SQL Edge backend
# ... run the demo (Proof, below) ...
docker compose down         # tear down when finished
```

---

## 1. The outbox table — [`OutboxDemo/OutboxMessage.cs`](OutboxDemo/OutboxMessage.cs)

One row per pending integration event, living in the *same* database as the domain table. The single source of truth for "has this been published?" is `ProcessedAt`: it stays `NULL` until the relay has **both** published to the broker **and** committed the stamp.

OutboxDemo/OutboxMessage.cs
```csharp
//   CREATE TABLE OutboxMessages (
//       Id          TEXT     NOT NULL PRIMARY KEY,  -- GUID; reused as the broker MessageId
//       Type        TEXT     NOT NULL,              -- event name, e.g. "QuoteCreated"
//       Payload     TEXT     NOT NULL,              -- JSON body of the event
//       OccurredAt  TEXT     NOT NULL,              -- when the domain change committed
//       ProcessedAt TEXT     NULL,                  -- NULL = not yet published+committed
//       Attempts    INTEGER  NOT NULL DEFAULT 0     -- how many times the relay tried to publish
//   );
public class OutboxMessage
{
    public Guid Id { get; set; }                      // == broker MessageId == idempotency key
    public string Type { get; set; } = "";
    public string Payload { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }  // the one bit that means "sent"
    public int Attempts { get; set; }
}
```

Both tables share one `DbContext` ([`OutboxDbContext.cs`](OutboxDemo/OutboxDbContext.cs)) — that co-location is what lets a single transaction cover both. `ProcessedAt` is indexed because the relay's hot query is "the unpublished rows, oldest first".

## 2. Producer — domain change + outbox row in ONE transaction — [`OutboxDemo/Producer.cs`](OutboxDemo/Producer.cs)

The crux of the pattern. The quote and the event-about-the-quote are inserted with the same `DbContext` and committed by a single `tx.CommitAsync()`. A crash anywhere before the commit rolls back **both** — no orphan quote, no orphan event.

OutboxDemo/Producer.cs
```csharp
await using var db = Db.Open();
await using var tx = await db.Database.BeginTransactionAsync();

// 1) the domain change
var quote = new Quote { Author = author, Text = text, CreatedAt = DateTimeOffset.UtcNow };
db.Quotes.Add(quote);
await db.SaveChangesAsync();          // assigns quote.Id (still inside the tx)

// 2) the outbox row describing that change — same DbContext, same tx
var evt = new QuoteCreated(quote.Id.ToString(), quote.Text, quote.Author);
db.OutboxMessages.Add(new OutboxMessage
{
    Id = Guid.NewGuid(),               // becomes the broker MessageId == idempotency key
    Type = nameof(QuoteCreated),
    Payload = JsonSerializer.Serialize(evt),
    OccurredAt = DateTimeOffset.UtcNow,
    ProcessedAt = null,                // unsent
});
await db.SaveChangesAsync();

// 3) commit BOTH rows atomically
await tx.CommitAsync();
```

## 3. The relay — publish, THEN mark sent — [`OutboxDemo/OutboxRelay.cs`](OutboxDemo/OutboxRelay.cs)

A separate step from the transaction. It polls for unsent rows, publishes each to the broker, and only then stamps `ProcessedAt`. **The ordering is the entire guarantee:**

```
publish to broker  ->  (CRASH WINDOW)  ->  mark sent in DB
```

* Crash **before** publish → row still unsent → retried next run. **No loss.**
* Crash **after** publish, **before** the mark commits → row still unsent → **published again** next run. Delivered at-least-once; the duplicate is the consumer's job. **No loss.**
* Crash **after** the mark commits → row is sent, never re-published. Done.

The forbidden ordering is the reverse — *mark sent, then publish* — because a crash in its window marks a message sent that never reached the broker: silent, permanent **loss**. So we always publish first and mark second.

OutboxDemo/OutboxRelay.cs
```csharp
var pending = (await db.OutboxMessages
        .Where(m => m.ProcessedAt == null)     // the unpublished rows...
        .ToListAsync())
    .OrderBy(m => m.OccurredAt).ToList();       // ...oldest first

foreach (var m in pending)
{
    m.Attempts++; await db.SaveChangesAsync();  // count the attempt before trying

    // ---- 1. PUBLISH ----  MessageId = the outbox Id, STABLE across re-publishes,
    //                       so the crash-copy and the retry-copy share an id.
    var msg = new ServiceBusMessage(m.Payload)
    {
        MessageId = m.Id.ToString(),
        ContentType = "application/json",
        Subject = m.Type,
    };
    await sender.SendMessageAsync(msg);

    // ---- 2. CRASH WINDOW ----  message is on the broker; the DB doesn't know yet.
    if (_crashAfterPublish) Environment.Exit(70);   // the mark below never runs

    // ---- 3. MARK SENT and commit ----
    m.ProcessedAt = DateTimeOffset.UtcNow;
    await db.SaveChangesAsync();
}
```

## 4. Idempotent consumer — turns at-least-once into effectively-once — [`OutboxDemo/Consumer.cs`](OutboxDemo/Consumer.cs)

Because the relay can re-publish, the consumer must be safe to run twice. It gates its side effect on a dedupe record keyed on `MessageId` (= the stable outbox `Id`), recorded **after** the work succeeds. A re-published copy carries the same id, so it is recognised as a duplicate and the side effect is skipped.

OutboxDemo/Consumer.cs
```csharp
var id = msg.MessageId;
if (_store.IsProcessed(_subscription, id))            // idempotency gate
{
    Console.WriteLine($"[{_id}] DUP   {id} -> already indexed, skipping side effect.");
    await args.CompleteMessageAsync(msg);
    return;
}
var quote = JsonSerializer.Deserialize<QuoteCreated>(msg.Body.ToString())!;
Console.WriteLine($"[{_id}] INDEX {id} -> {quote.Author}: \"{quote.Text}\"");  // the once-only effect
_store.MarkProcessed(_subscription, id, _id);          // record AFTER success
await args.CompleteMessageAsync(msg);
```

The dedupe store ([`IdempotencyStore.cs`](OutboxDemo/IdempotencyStore.cs)) is a SQLite table keyed `PRIMARY KEY (scope, message_id)` with `INSERT OR IGNORE` — the local stand-in for a `UNIQUE`-constrained row a real consumer service keeps in its own database.

---

## 5. Proof — crash between publish and mark-sent, no message lost or duplicated

**The crash scenario tested:** `relay --crash` publishes the message to the broker and then calls `Environment.Exit(70)` *before* `ProcessedAt` is committed — exactly the dangerous window where the broker has the message but the DB doesn't yet know. This is the case that, with naive dual writes, would either lose the event or send it without a record.

**Why no message is lost:** the mark never committed, so the row stays `UNSENT`. The next relay run finds it and **re-publishes** — at-least-once delivery. The event reaches the broker no matter when the crash lands.

**Why nothing is double-processed:** both the crash-copy and the retry-copy carry the **same `MessageId`** (the stable outbox `Id`). The consumer indexes the first (`INDEX`) and recognises the second as a duplicate (`DUP`), skipping the side effect — effectively-once processing.

### Output

Three terminals: the broker, a long-running **idempotent consumer** on `search-index`, and a **driver** that produces one quote and runs the relay twice — once crashing in the window, once recovering.

**Terminal 1 — Docker** · bring up the emulator + its SQL backend
```powershell
cd OutboxDemo
docker compose up -d
```

**Terminal 2 — Idempotent consumer** · started fresh, stays up; indexes copy #1, dedupes copy #2
```powershell
dotnet run -- consume search-index C1
[C1] idempotent consumer up on 'search-index'. Ctrl+C to stop.
[C1] INDEX d2ced2c7-6f3f-431d-b202-d53d0a00a74d (delivery#1) -> Edsger Dijkstra: "Simplicity is prerequisite for reliability."
[C1] DUP   d2ced2c7-6f3f-431d-b202-d53d0a00a74d (delivery#1) -> already indexed, skipping side effect.
```

**Terminal 3 — Driver** · reset → produce → relay (crash) → outbox → relay (recovery)
```powershell
dotnet run -- reset
reset: deleted outbox.db and idempotency.db.

dotnet run -- produce "Edsger Dijkstra" "Simplicity is prerequisite for reliability."
COMMIT  quote #1 + outbox d2ced2c7-6f3f-431d-b202-d53d0a00a74d in one transaction.
        "Simplicity is prerequisite for reliability." — Edsger Dijkstra
        outbox row is UNSENT (ProcessedAt = NULL).

dotnet run -- relay --crash
relay: 1 unsent row(s) to publish.
relay: PUBLISHED   d2ced2c7-6f3f-431d-b202-d53d0a00a74d (attempt #1) -> topic 'quotes'
relay: *** CRASH *** after publishing d2ced2c7-6f3f-431d-b202-d53d0a00a74d, before marking it sent.
# process exits with code 70 — the mark never ran

dotnet run -- outbox            # mark never committed → still UNSENT
Id                                     Type           Attempts  State   ProcessedAt
----------------------------------------------------------------------------------------------------
d2ced2c7-6f3f-431d-b202-d53d0a00a74d   QuoteCreated   1         UNSENT  (null)

dotnet run -- relay             # recovery: re-publishes copy #2 (same id), then marks sent
relay: 1 unsent row(s) to publish.
relay: PUBLISHED   d2ced2c7-6f3f-431d-b202-d53d0a00a74d (attempt #2) -> topic 'quotes'
relay: MARKED SENT d2ced2c7-6f3f-431d-b202-d53d0a00a74d (ProcessedAt set).
relay: batch complete.
```

The chain reads cleanly: one transaction (`COMMIT quote #1 + outbox`), a publish-then-crash that leaves the row `UNSENT` (Attempts=1), a recovery run that re-publishes (Attempts=2) and finally stamps `SENT` (`MARKED SENT`), and a consumer that applies the side effect exactly once (`INDEX`) and discards the at-least-once duplicate (`DUP`). The message was neither lost nor processed twice.

### Output Screenshots

**Broker** — Azure Service Bus emulator boots and loads topic `quotes` / subscription `search-index`

![broker](broker.png)

**Driver** — one-transaction commit → crash (UNSENT) → recovery (MARKED SENT)

![driver](driver.png)

**Idempotent consumer** — INDEX (copy #1) then DUP (copy #2 deduped)

![consumer](consumer.png)

## What did I learn?
You cannot make a database commit and a broker publish atomic — they are two systems. The outbox pattern sidesteps that by turning the publish into a second row in the *same* transaction, so the only thing that can diverge is "is this row published yet", and that is recoverable: the relay just retries. The price is that retries cause duplicates, so the publish must be at-least-once and the consumer must be idempotent. The stable `MessageId` is the hinge — it is set inside the transaction, survives every re-publish, and is what lets the consumer tell "the same event again" from "a new event".

## What can break this?
The ordering in the relay is load-bearing: if you mark-sent before publishing, a crash in that window loses the message forever and no retry will ever fix it. A second risk is the consumer's dedupe record and its side effect not being atomic — if it applies the effect, then crashes before writing the `processed_messages` row, a redelivery runs the effect twice. In this demo the effect is just a console write, but a real consumer should write the business change and the dedupe row in one local transaction (or make the effect naturally idempotent, e.g. an upsert) to truly get effectively-once.
