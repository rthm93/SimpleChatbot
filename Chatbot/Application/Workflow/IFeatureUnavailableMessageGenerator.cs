namespace Chatbot.Application.Workflow;

public interface IFeatureUnavailableMessageGenerator
{
    Task<string> GenerateAsync(string featureName, CancellationToken cancellationToken);
}
