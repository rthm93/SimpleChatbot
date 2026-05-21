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

    private const string MalformedPayloadMessage = "Malformed WAHA webhook payload.";

    private static async Task<Results<Ok, BadRequest<string>>> HandleMessageAsync(
        WahaWebhookRequest request,
        WahaWebhookAdapter adapter,
        WahaWebhookMessageProcessor processor,
        CancellationToken cancellationToken)
    {
        if (!adapter.TryNormalize(request, out var message) || message is null)
        {
            return TypedResults.BadRequest(MalformedPayloadMessage);
        }

        await processor.ProcessMessageAsync(message, cancellationToken);
        return TypedResults.Ok();
    }

    private static async Task<Results<Ok, BadRequest<string>>> HandleMessageAnyAsync(
        WahaWebhookRequest request,
        WahaWebhookAdapter adapter,
        WahaWebhookMessageProcessor processor,
        CancellationToken cancellationToken)
    {
        if (!adapter.TryNormalize(request, out var message) || message is null)
        {
            return TypedResults.BadRequest(MalformedPayloadMessage);
        }

        await processor.ProcessAnyMessageAsync(message, cancellationToken);
        return TypedResults.Ok();
    }
}
