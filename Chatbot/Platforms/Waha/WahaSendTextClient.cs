using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Chatbot.Platforms.Waha;

public sealed class WahaSendTextClient(HttpClient httpClient, IOptions<WahaOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string?> SendTextAsync(string chatId, string text, CancellationToken cancellationToken)
    {
        var body = new
        {
            session = options.Value.Session,
            chatId,
            text
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/sendText")
        {
            Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
        };

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            request.Headers.Add("X-Api-Key", options.Value.ApiKey);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength == 0)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return TryFindString(document.RootElement, "id")
            ?? TryFindString(document.RootElement, "messageId");
    }

    private static string? TryFindString(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals(propertyName) && property.Value.ValueKind == JsonValueKind.String)
                {
                    return property.Value.GetString();
                }

                var nested = TryFindString(property.Value, propertyName);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = TryFindString(item, propertyName);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }

        return null;
    }
}
