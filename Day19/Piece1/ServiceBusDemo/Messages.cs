namespace ServiceBusDemo;

/// The event payload carried on the topic. QuoteId doubles as the business key;
/// it is copied onto ServiceBusMessage.MessageId so it can serve as the
/// idempotency key on the consumer side.
public record QuoteCreated(string QuoteId, string Text, string Author);
