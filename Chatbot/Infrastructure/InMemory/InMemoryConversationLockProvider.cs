using System.Collections.Concurrent;
using Chatbot.Application;
using Chatbot.Domain;

namespace Chatbot.Infrastructure.InMemory;

public sealed class InMemoryConversationLockProvider : IConversationLockProvider
{
    private readonly ConcurrentDictionary<ConversationKey, SemaphoreSlim> _locks = new();

    public async ValueTask<IAsyncDisposable> AcquireAsync(ConversationKey key, CancellationToken cancellationToken)
    {
        var semaphore = _locks.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);
        return new Releaser(semaphore);
    }

    private sealed class Releaser(SemaphoreSlim semaphore) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            semaphore.Release();
            return ValueTask.CompletedTask;
        }
    }
}
