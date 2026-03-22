using System.Collections.Concurrent;

namespace MusicGrabber.Shared;

public sealed class InProcessEventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();

    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : class
    {
        var handlers = _handlers.GetOrAdd(typeof(TEvent), _ => []);
        lock (handlers)
        {
            handlers.Add(handler);
        }
    }

    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken ct = default)
        where TEvent : class
    {
        if (!_handlers.TryGetValue(typeof(TEvent), out var handlers))
            return;

        List<Delegate> snapshot;
        lock (handlers)
        {
            snapshot = [.. handlers];
        }

        foreach (var handler in snapshot)
        {
            await ((Func<TEvent, CancellationToken, Task>)handler)(domainEvent, ct);
        }
    }
}
