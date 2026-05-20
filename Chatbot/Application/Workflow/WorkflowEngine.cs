using Chatbot.Domain;
using Microsoft.Extensions.Options;

namespace Chatbot.Application.Workflow;

public sealed class WorkflowEngine(
    IConversationStateStore conversationStateStore,
    IWorkflowStateStore workflowStateStore,
    IConversationLockProvider lockProvider,
    IOutboundMessageSender outboundMessageSender,
    IWorkflowDefinitionProvider workflowDefinitionProvider,
    TimeProvider timeProvider,
    IOptions<WorkflowOptions> options)
{
    public async Task ProcessCustomerMessageAsync(NormalizedMessage message, CancellationToken cancellationToken)
    {
        await using var acquiredLock = await lockProvider.AcquireAsync(message.Conversation, cancellationToken);

        var now = timeProvider.GetUtcNow();
        var command = NormalizeCommand(message.Text);
        var conversationState = await conversationStateStore.GetAsync(message.Conversation, cancellationToken)
            ?? new ConversationStateRecord(ConversationState.Idle, now);

        if (command == "talk to human")
        {
            await workflowStateStore.ClearAsync(message.Conversation, cancellationToken);
            await conversationStateStore.SetAsync(
                message.Conversation,
                new ConversationStateRecord(ConversationState.HumanTookOver, now),
                cancellationToken);
            return;
        }

        if (conversationState.State == ConversationState.HumanTookOver
            && !IsExpired(conversationState.UpdatedAt, now, options.Value.HumanTookOverTimeoutMinutes))
        {
            return;
        }

        if (command == "restart")
        {
            await StartWorkflowAsync(message.Conversation, now, cancellationToken);
            return;
        }

        var workflowState = await workflowStateStore.GetAsync(message.Conversation, cancellationToken);
        if (conversationState.State != ConversationState.InProgress || workflowState is null)
        {
            await StartWorkflowAsync(message.Conversation, now, cancellationToken);
            return;
        }

        if (IsExpired(workflowState.UpdatedAt, now, options.Value.WorkflowTimeoutMinutes))
        {
            await StartWorkflowAsync(message.Conversation, now, cancellationToken);
            return;
        }

        var definition = workflowDefinitionProvider.GetLatest();
        var block = definition.GetBlock(workflowState.BlockId);
        var result = await block.HandleAsync(message, cancellationToken);

        await workflowStateStore.SetAsync(
            message.Conversation,
            workflowState with { BlockId = result.NextBlockId, UpdatedAt = now },
            cancellationToken);
        await conversationStateStore.SetAsync(
            message.Conversation,
            new ConversationStateRecord(ConversationState.InProgress, now),
            cancellationToken);
        await outboundMessageSender.SendTextAsync(message.Conversation, result.ReplyText, cancellationToken);
    }

    private async Task StartWorkflowAsync(ConversationKey conversation, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var definition = workflowDefinitionProvider.GetLatest();

        await workflowStateStore.SetAsync(
            conversation,
            new WorkflowStateRecord(definition.Id, definition.FirstBlockId, now, now),
            cancellationToken);
        await conversationStateStore.SetAsync(
            conversation,
            new ConversationStateRecord(ConversationState.InProgress, now),
            cancellationToken);
        await outboundMessageSender.SendTextAsync(conversation, MvpWorkflowDefinitionProvider.MenuPrompt, cancellationToken);
    }

    private static string? NormalizeCommand(string text)
    {
        var trimmed = text.Trim();
        return trimmed.Equals("talk to human", StringComparison.OrdinalIgnoreCase) ? "talk to human"
            : trimmed.Equals("restart", StringComparison.OrdinalIgnoreCase) ? "restart"
            : null;
    }

    private static bool IsExpired(DateTimeOffset updatedAt, DateTimeOffset now, int timeoutMinutes) =>
        now - updatedAt >= TimeSpan.FromMinutes(timeoutMinutes);
}
