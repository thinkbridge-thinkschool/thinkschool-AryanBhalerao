using OutboxDemo;

// Well-known emulator dev connection string (same as Day 19). UseDevelopmentEmulator=true
// tells the SDK to skip TLS and use the emulator's fixed dev SAS key.
const string ConnString =
    "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

var idempotencyDbPath = Path.Combine(AppContext.BaseDirectory, "idempotency.db");

var cmd = args.Length > 0 ? args[0].ToLowerInvariant() : "help";
switch (cmd)
{
    // produce "<author>" "<text>"  — domain change + outbox row in ONE transaction
    case "produce":
    {
        var author = args.Length > 1 ? args[1] : "Kent Beck";
        var text   = args.Length > 2 ? args[2] : "Make it work, make it right, make it fast.";
        await Producer.RunAsync(author, text);
        break;
    }

    // relay [--crash]  — publish unsent rows, then mark sent. --crash dies in the window between.
    case "relay":
    {
        var crash = args.Contains("--crash");
        await new OutboxRelay(ConnString, crash).RunOnceAsync();
        break;
    }

    // outbox  — dump the outbox table (UNSENT / SENT)
    case "outbox":
        await Inspector.RunAsync();
        break;

    // consume [subscription] [id]  — long-running idempotent consumer
    case "consume":
    {
        var sub = args.Length > 1 ? args[1] : "search-index";
        var id  = args.Length > 2 ? args[2] : $"C-{Environment.ProcessId}";
        var store = new IdempotencyStore(idempotencyDbPath);
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
        await new Consumer(ConnString, sub, id, store).RunAsync(cts.Token);
        break;
    }

    // reset  — delete both SQLite files for a clean demo run
    case "reset":
        foreach (var p in new[] { Db.Path, idempotencyDbPath })
            foreach (var f in new[] { p, p + "-wal", p + "-shm" })
                if (File.Exists(f)) File.Delete(f);
        Console.WriteLine("reset: deleted outbox.db and idempotency.db.");
        break;

    default:
        Console.WriteLine("""
            OutboxDemo — transactional outbox over EF Core (SQLite) + Azure Service Bus emulator

            Usage:
              dotnet run -- produce "<author>" "<text>"
                    Write a quote AND an outbox row in one EF transaction.

              dotnet run -- relay [--crash]
                    Publish every unsent outbox row to topic 'quotes', then mark it sent.
                    --crash simulates a process death AFTER publishing, BEFORE marking sent.

              dotnet run -- outbox
                    Print the outbox table (UNSENT / SENT, attempt counts).

              dotnet run -- consume [subscription] [id]
                    Run the idempotent consumer (dedupes on MessageId). e.g.:
                      dotnet run -- consume search-index C1

              dotnet run -- reset
                    Delete the local SQLite files for a clean run.
            """);
        break;
}
