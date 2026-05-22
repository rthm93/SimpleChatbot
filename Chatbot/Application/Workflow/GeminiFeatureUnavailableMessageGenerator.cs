using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Options;

namespace Chatbot.Application.Workflow;

public sealed class GeminiFeatureUnavailableMessageGenerator(
    IOptions<GeminiOptions> options) : IFeatureUnavailableMessageGenerator
{
    public async Task<string> GenerateAsync(string featureName, CancellationToken cancellationToken)
    {
        var apiKey = options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return GetFallbackMessage(featureName);
        }

        try
        {
            var client = new Client(apiKey: apiKey);
            var config = new GenerateContentConfig
            {
                SystemInstruction = new Content
                {
                    Parts =
                    [
                        new Part
                        {
                            Text = "Write one short, playful customer support message. Tell the user the requested feature is not implemented yet. Do not apologize more than once. Keep it under 25 words."
                        }
                    ]
                },
                Temperature = 0.8,
                MaxOutputTokens = 50
            };

            var response = await client.Models.GenerateContentAsync(
                model: options.Value.Model,
                contents: $"Feature: {featureName}",
                config: config,
                cancellationToken: cancellationToken);

            return response.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Trim()
                is { Length: > 0 } text
                ? text
                : GetFallbackMessage(featureName);
        }
        catch
        {
            return GetFallbackMessage(featureName);
        }
    }

    private static string GetFallbackMessage(string featureName) =>
        $"{featureName} is still backstage learning its lines. Please check back soon.";
}
