using Chatbot.Domain;

namespace Chatbot.Application.Workflow;

public sealed class MvpWorkflowDefinitionProvider(
    IFeatureUnavailableMessageGenerator featureUnavailableMessageGenerator) : IWorkflowDefinitionProvider
{
    public const string MenuPrompt = """
        Hi, how can I help you?
        1. Make appointment
        2. Enquiry
        """;

    private readonly WorkflowDefinition _definition = new(
        "mvp",
        MenuBlock.BlockId,
        [new MenuBlock(featureUnavailableMessageGenerator)]);

    public WorkflowDefinition GetLatest() => _definition;

    private sealed class MenuBlock(
        IFeatureUnavailableMessageGenerator featureUnavailableMessageGenerator) : IWorkflowBlock
    {
        public const string BlockId = "menu";

        public string Id => BlockId;

        public async Task<WorkflowBlockResult> HandleAsync(NormalizedMessage message, CancellationToken cancellationToken)
        {
            var reply = message.Text.Trim() switch
            {
                "1" => await featureUnavailableMessageGenerator.GenerateAsync("Make appointment", cancellationToken),
                "2" => await featureUnavailableMessageGenerator.GenerateAsync("Enquiry", cancellationToken),
                _ => MenuPrompt
            };

            return new WorkflowBlockResult(reply, BlockId);
        }
    }
}
