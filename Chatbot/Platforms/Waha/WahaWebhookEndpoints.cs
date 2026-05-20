using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Chatbot.Platforms.Waha;

public static class WahaWebhookEndpoints
{
    public static IEndpointRouteBuilder MapWahaWebhookEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/waha/message", HandleMessageAsync);
        endpoints.MapPost("/api/waha/message-any", HandleMessageAnyAsync);
        return endpoints;
    }

    private static async Task<Results<Ok, BadRequest>> HandleMessageAsync(
        JsonElement payload,
        WahaWebhookAdapter adapter,
        WahaWebhookMessageProcessor processor,
        CancellationToken cancellationToken)
    {
        if (!adapter.TryNormalize(payload, out var message) || message is null)
        {
            return TypedResults.BadRequest();
        }

        await processor.ProcessMessageAsync(message, cancellationToken);
        return TypedResults.Ok();
    }

    private static async Task<Results<Ok, BadRequest>> HandleMessageAnyAsync(
        JsonElement payload,
        WahaWebhookAdapter adapter,
        WahaWebhookMessageProcessor processor,
        CancellationToken cancellationToken)
    {
        if (!adapter.TryNormalize(payload, out var message) || message is null)
        {
            return TypedResults.BadRequest();
        }

        await processor.ProcessAnyMessageAsync(message, cancellationToken);
        return TypedResults.Ok();
    }
}
