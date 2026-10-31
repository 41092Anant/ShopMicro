namespace Notification.Worker.Services;

/// <summary>
/// Abstraction for tracking processed message or entity identifiers to guarantee
/// idempotent message handling across distributed at-least-once delivery brokers.
/// </summary>
public interface IProcessedMessageStore
{
    /// <summary>
    /// Attempts to record a message key as processed.
    /// Returns true if this is the first time the key is seen (message should be processed).
    /// Returns false if the key was already recorded (duplicate delivery; message should be skipped).
    /// </summary>
    Task<bool> TryAddAsync(string messageKey, CancellationToken cancellationToken = default);
}
