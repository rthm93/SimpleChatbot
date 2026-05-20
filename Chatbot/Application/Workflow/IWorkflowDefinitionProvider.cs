namespace Chatbot.Application.Workflow;

public interface IWorkflowDefinitionProvider
{
    WorkflowDefinition GetLatest();
}
