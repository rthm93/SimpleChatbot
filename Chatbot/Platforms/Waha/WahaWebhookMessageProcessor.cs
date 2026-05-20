using Chatbot.Application;
using Chatbot.Application.Workflow;
using Chatbot.Domain;

namespace Chatbot.Platforms.Waha;

public sealed class WahaWebhookMessageProcessor(
    WorkflowEngine workflowEngine,
    IConversationStateStore conversationStateStore,
    IWorkflowStateStore workflowStateStore,
    IConversationLockProvider lockProvider,
    IWahaAppMessageStore appMessageStore,
    TimeProvider timeProvider)
{
    public Task ProcessMessageAsync(NormalizedMessage message, CancellationToken cancellationToken)
    {
        if (message.Direction == MessageDirection.BotToCustomer)
        {
            return Task.CompletedTask;
        }

        return workflowEngine.ProcessCustomerMessageAsync(message, cancellationToken);
    }

    public async Task ProcessAnyMessageAsync(NormalizedMessage message, CancellationToken cancellationToken)
    {
        if (message.Direction != MessageDirection.BotToCustomer)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(message.ExternalMessageId)
            && await appMessageStore.ContainsSentMessageIdAsync(message.ExternalMessageId, cancellationToken))
        {
            return;
        }

        await using var acquiredLock = await lockProvider.AcquireAsync(message.Conversation, cancellationToken);
        await workflowStateStore.ClearAsync(message.Conversation, cancellationToken);
        await conversationStateStore.SetAsync(
            message.Conversation,
            new ConversationStateRecord(ConversationState.HumanTookOver, timeProvider.GetUtcNow()),
            cancellationToken);
    }
}
