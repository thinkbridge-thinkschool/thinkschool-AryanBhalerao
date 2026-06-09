using Microsoft.Data.Sqlite;

namespace ServiceBusDemo;

/// A handler is *idempotent* when applying the same message twice has the same
/// effect as applying it once. Service Bus is at-least-once: redeliveries and
/// duplicate sends happen, so the consumer must dedupe. We do that with a small
/// dedupe table keyed by (scope, MessageId).
///
/// The SCOPE is the subscription (consumer group). It matters: a topic fans the
/// same MessageId out to every subscription, and each is an independent consumer
/// that must process its own copy. Keying on MessageId alone would make the
/// second subscription skip everything the first already did. So the key is
/// (subscription, message_id).
///
/// SQLite on a shared file (WAL mode + busy_timeout) gives us a dedupe store
/// that two competing worker *processes* share — exactly what a real consumer
/// would get from a row in Postgres/SQL Server with a UNIQUE constraint.
public sealed class IdempotencyStore
{
    private readonly string _connString;

    public IdempotencyStore(string dbPath)
    {
        _connString = $"Data Source={dbPath};Cache=Shared";
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS processed_messages (
                scope        TEXT NOT NULL,
                message_id   TEXT NOT NULL,
                processed_at TEXT NOT NULL,
                worker       TEXT NOT NULL,
                PRIMARY KEY (scope, message_id)
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connString);
        conn.Open();
        using var pragma = conn.CreateCommand();
        // WAL lets readers and one writer coexist; busy_timeout makes concurrent
        // writers from the two worker processes wait instead of erroring.
        pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
        pragma.ExecuteNonQuery();
        return conn;
    }

    /// Has this MessageId already been successfully handled *within this scope*?
    public bool IsProcessed(string scope, string messageId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM processed_messages WHERE scope = $s AND message_id = $id LIMIT 1;";
        cmd.Parameters.AddWithValue("$s", scope);
        cmd.Parameters.AddWithValue("$id", messageId);
        return cmd.ExecuteScalar() is not null;
    }

    /// Record success. INSERT OR IGNORE so two competing workers that race on the
    /// same (scope, id) can't both insert — the second is a no-op, never an error.
    public void MarkProcessed(string scope, string messageId, string worker)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR IGNORE INTO processed_messages (scope, message_id, processed_at, worker)
            VALUES ($s, $id, $ts, $w);
            """;
        cmd.Parameters.AddWithValue("$s", scope);
        cmd.Parameters.AddWithValue("$id", messageId);
        cmd.Parameters.AddWithValue("$ts", DateTimeOffset.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("$w", worker);
        cmd.ExecuteNonQuery();
    }
}
