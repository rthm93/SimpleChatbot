using Chatbot.Domain;

namespace Chatbot.Application;

public interface IWorkflowStateStore
{
    ValueTask<WorkflowStateRecord?> GetAsync(ConversationKey key, CancellationToken cancellationToken);

    ValueTask SetAsync(ConversationKey key, WorkflowStateRecord state, CancellationToken cancellationToken);

    ValueTask ClearAsync(ConversationKey key, CancellationToken cancellationToken);
}
