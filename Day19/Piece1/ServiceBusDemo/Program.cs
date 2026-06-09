using ServiceBusDemo;

// Well-known emulator dev connection string. UseDevelopmentEmulator=true tells
// the SDK to skip TLS and use the emulator's fixed dev SAS key (the literal
// "SAS_KEY_VALUE" is recognized in this mode).
const string ConnString =
    "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";

// Shared dedupe DB lives next to the binary so two worker processes share it.
var dbPath = Path.Combine(AppContext.BaseDirectory, "idempotency.db");

var cmd = args.Length > 0 ? args[0].ToLowerInvariant() : "help";
switch (cmd)
{
    case "publish":
        await new Publisher(ConnString).RunAsync();
        break;

    case "worker":
    {
        var sub         = args.Length > 1 ? args[1] : "search-index";
        var concurrency = args.Length > 2 && int.TryParse(args[2], out var c) ? c : 1;
        var workerId    = args.Length > 3 ? args[3] : $"w-{Environment.ProcessId}";

        var store = new IdempotencyStore(dbPath);
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        await new Worker(ConnString, sub, concurrency, workerId, store).RunAsync(cts.Token);
        break;
    }

    case "dlq":
    {
        var sub = args.Length > 1 ? args[1] : "search-index";
        await new DlqReader(ConnString, sub).RunAsync();
        break;
    }

    default:
        Console.WriteLine("""
            ServiceBusDemo — local Azure Service Bus (emulator) demo

            Usage:
              dotnet run -- publish
                    Send 5 unique + 1 duplicate + 1 poison message to topic 'quotes'.

              dotnet run -- worker <subscription> <concurrency> <workerId>
                    Run a competing consumer. e.g.:
                      dotnet run -- worker search-index 4 A
                      dotnet run -- worker search-index 4 B   (second instance, competes)

              dotnet run -- dlq <subscription>
                    Inspect the subscription's dead-letter queue.
            """);
        break;
}
