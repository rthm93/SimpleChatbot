using System.Collections.Concurrent;
using Chatbot.Application;
using Chatbot.Domain;

namespace Chatbot.Infrastructure.InMemory;

public sealed class InMemoryWorkflowStateStore : IWorkflowStateStore
{
    private readonly ConcurrentDictionary<ConversationKey, WorkflowStateRecord> _states = new();

    public ValueTask<WorkflowStateRecord?> GetAsync(ConversationKey key, CancellationToken cancellationToken) =>
        ValueTask.FromResult(_states.TryGetValue(key, out var state) ? state : null);

    public ValueTask SetAsync(ConversationKey key, WorkflowStateRecord state, CancellationToken cancellationToken)
    {
        _states[key] = state;
        return ValueTask.CompletedTask;
    }

    public ValueTask ClearAsync(ConversationKey key, CancellationToken cancellationToken)
    {
        _states.TryRemove(key, out _);
        return ValueTask.CompletedTask;
    }
}
