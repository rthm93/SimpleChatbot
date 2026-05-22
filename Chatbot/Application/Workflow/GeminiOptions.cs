namespace Chatbot.Application.Workflow;

public sealed class GeminiOptions
{
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "gemini-2.0-flash";
}
