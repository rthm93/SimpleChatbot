namespace Chatbot.Application.Workflow;

public sealed class WorkflowDefinition
{
    private readonly IReadOnlyDictionary<string, IWorkflowBlock> _blocks;

    public WorkflowDefinition(string id, string firstBlockId, IEnumerable<IWorkflowBlock> blocks)
    {
        Id = id;
        FirstBlockId = firstBlockId;
        _blocks = blocks.ToDictionary(block => block.Id, StringComparer.Ordinal);
    }

    public string Id { get; }

    public string FirstBlockId { get; }

    public IWorkflowBlock GetBlock(string blockId) => _blocks[blockId];
}
