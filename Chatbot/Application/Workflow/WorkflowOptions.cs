namespace Chatbot.Application.Workflow;

public sealed class WorkflowOptions
{
    public int WorkflowTimeoutMinutes { get; set; } = 10;

    public int HumanTookOverTimeoutMinutes { get; set; } = 30;
}
