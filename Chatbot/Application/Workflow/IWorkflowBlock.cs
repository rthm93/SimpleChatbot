using Chatbot.Domain;

namespace Chatbot.Application.Workflow;

public interface IWorkflowBlock
{
    string Id { get; }

    Task<WorkflowBlockResult> HandleAsync(NormalizedMessage message, CancellationToken cancellationToken);
}
