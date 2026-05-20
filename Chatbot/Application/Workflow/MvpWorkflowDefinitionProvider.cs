using Chatbot.Domain;

namespace Chatbot.Application.Workflow;

public sealed class MvpWorkflowDefinitionProvider : IWorkflowDefinitionProvider
{
    public const string MenuPrompt = """
        Hi, how can I help you?
        1. Make appointment
        2. Enquiry
        """;

    private static readonly WorkflowDefinition Definition = new(
        "mvp",
        MenuBlock.BlockId,
        [new MenuBlock()]);

    public WorkflowDefinition GetLatest() => Definition;

    private sealed class MenuBlock : IWorkflowBlock
    {
        public const string BlockId = "menu";

        public string Id => BlockId;

        public Task<WorkflowBlockResult> HandleAsync(NormalizedMessage message, CancellationToken cancellationToken)
        {
            var reply = message.Text.Trim() switch
            {
                "1" => "Sorry make appointment feature is not available yet",
                "2" => "Sorry enquiry feature is not available yet",
                _ => MenuPrompt
            };

            return Task.FromResult(new WorkflowBlockResult(reply, BlockId));
        }
    }
}
