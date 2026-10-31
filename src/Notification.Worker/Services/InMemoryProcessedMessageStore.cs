namespace Notification.Worker.Services;

using System.Collections.Concurrent;

/// <summary>
/// Thread-safe in-memory store for idempotency tracking.
/// In production microservices, this can be swapped with a distributed cache (e.g. Redis)
/// or a relational database table with unique constraints on the message/entity key.
/// </summary>
public class InMemoryProcessedMessageStore : IProcessedMessageStore
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _processedKeys = new();

    public Task<bool> TryAddAsync(string messageKey, CancellationToken cancellationToken = default)
    {
        var added = _processedKeys.TryAdd(messageKey, DateTimeOffset.UtcNow);
        return Task.FromResult(added);
    }
}
