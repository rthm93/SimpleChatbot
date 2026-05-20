namespace Chatbot.Application;

public sealed record WorkflowStateRecord(
    string WorkflowId,
    string BlockId,
    DateTimeOffset StartedAt,
    DateTimeOffset UpdatedAt);
