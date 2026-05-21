using System.Globalization;
using System.Text.Json;
using Chatbot.Domain;

namespace Chatbot.Platforms.Waha;

public sealed class WahaWebhookAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public bool TryNormalize(WahaWebhookRequest source, out NormalizedMessage? message)
    {
        message = null;

        var payload = source.Payload;
        if (payload is null
            || payload.FromMe is not { } fromMe
            || !TryGetTimestamp(payload.Timestamp, out var timestamp)
            || !TryGetString(payload.Body, out var text))
        {
            return false;
        }

        var contactId = fromMe
            ? FirstNonEmpty(payload.To, payload.Data?.To, payload.Data?.Id?.Remote)
            : FirstNonEmpty(payload.From, payload.Data?.From, payload.Data?.Id?.Remote);

        if (string.IsNullOrWhiteSpace(contactId))
        {
            return false;
        }

        var id = FirstNonEmpty(ReadString(payload.Id), payload.Data?.Id?.Serialized, payload.Data?.Id?.Id);
        message = new NormalizedMessage(
            new ConversationKey(Platform.Waha, contactId),
            fromMe ? MessageDirection.BotToCustomer : MessageDirection.CustomerToBot,
            text,
            timestamp,
            id,
            JsonSerializer.SerializeToElement(source, JsonOptions));
        return true;
    }

    private static bool TryGetString(string? source, out string value)
    {
        value = source ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? ReadString(JsonElement? source)
    {
        if (source is not { } element)
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString();
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (element.TryGetProperty("_serialized", out var serialized)
            && serialized.ValueKind == JsonValueKind.String)
        {
            return serialized.GetString();
        }

        if (element.TryGetProperty("id", out var id)
            && id.ValueKind == JsonValueKind.String)
        {
            return id.GetString();
        }

        return null;
    }

    private static bool TryGetTimestamp(JsonElement? source, out DateTimeOffset value)
    {
        value = default;

        if (source is not { } property)
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var numericValue))
        {
            value = DateTimeOffset.FromUnixTimeMilliseconds((long)(numericValue * 1000));
            return true;
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            var text = property.GetString();
            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixSeconds))
            {
                value = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
                return true;
            }

            if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out value))
            {
                return true;
            }
        }

        return false;
    }
}
