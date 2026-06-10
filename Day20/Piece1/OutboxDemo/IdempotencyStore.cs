using Microsoft.Data.Sqlite;

namespace OutboxDemo;

/// Consumer-side dedupe. Service Bus is at-least-once, and the outbox relay makes
/// that concrete: a crash between publish and mark-sent causes the SAME message
/// (same MessageId) to be published twice. So the consumer must be idempotent —
/// applying a message twice has the same effect as once.
///
/// It records each successfully-handled MessageId, keyed by (scope, message_id)
/// where scope is the subscription. The record is written AFTER the side effect
/// and checked before it, so a redelivery of an already-applied message skips the
/// work. A separate SQLite file (not the outbox DB) stands in for the row a real
/// consumer service would keep in its own database with a UNIQUE constraint.
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
        pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
        pragma.ExecuteNonQuery();
        return conn;
    }

    /// Has this MessageId already been successfully handled within this scope?
    public bool IsProcessed(string scope, string messageId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM processed_messages WHERE scope = $s AND message_id = $id LIMIT 1;";
        cmd.Parameters.AddWithValue("$s", scope);
        cmd.Parameters.AddWithValue("$id", messageId);
        return cmd.ExecuteScalar() is not null;
    }

    /// Record success. INSERT OR IGNORE so a race can't double-insert; the second
    /// writer is a no-op rather than an error.
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
