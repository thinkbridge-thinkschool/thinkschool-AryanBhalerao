namespace OutboxDemo;

/// One pending integration event, written to the SAME database (and in the SAME
/// transaction) as the domain change it describes. The relay later reads these,
/// publishes them to the broker, and stamps ProcessedAt.
///
///   CREATE TABLE OutboxMessages (
///       Id          TEXT     NOT NULL PRIMARY KEY,  -- GUID; reused as the broker MessageId
///       Type        TEXT     NOT NULL,              -- event name, e.g. "QuoteCreated"
///       Payload     TEXT     NOT NULL,              -- JSON body of the event
///       OccurredAt  TEXT     NOT NULL,              -- when the domain change committed
///       ProcessedAt TEXT     NULL,                  -- NULL = not yet published+committed
///       Attempts    INTEGER  NOT NULL DEFAULT 0     -- how many times the relay tried to publish
///   );
///
/// The single source of truth for "has this been published?" is ProcessedAt.
/// It is NULL until the relay has BOTH published to the broker AND committed the
/// stamp. That ordering is the whole guarantee (see OutboxRelay).
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "";
    public string Payload { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int Attempts { get; set; }
}
