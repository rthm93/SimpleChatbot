using System.Globalization;
using System.Text.Json;
using Chatbot.Domain;

namespace Chatbot.Platforms.Waha;

public sealed class WahaWebhookAdapter
{
    public bool TryNormalize(JsonElement source, out NormalizedMessage? message)
    {
        message = null;

        var payload = source.TryGetProperty("payload", out var payloadElement)
            && payloadElement.ValueKind == JsonValueKind.Object
                ? payloadElement
                : source;

        if (!TryGetBoolean(payload, "fromMe", out var fromMe)
            || !TryGetTimestamp(payload, "timestamp", out var timestamp)
            || !TryGetString(payload, "body", out var text))
        {
            return false;
        }

        var contactId = fromMe
            ? TryGetString(payload, "to", out var to) ? to : null
            : TryGetString(payload, "from", out var from) ? from : null;

        if (string.IsNullOrWhiteSpace(contactId))
        {
            return false;
        }

        TryGetString(payload, "id", out var id);
        message = new NormalizedMessage(
            new ConversationKey(Platform.Waha, contactId),
            fromMe ? MessageDirection.BotToCustomer : MessageDirection.CustomerToBot,
            text,
            timestamp,
            id,
            source.Clone());
        return true;
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;

        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryGetBoolean(JsonElement element, string propertyName, out bool value)
    {
        value = false;

        if (!element.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.True || property.ValueKind == JsonValueKind.False)
        {
            value = property.GetBoolean();
            return true;
        }

        return false;
    }

    private static bool TryGetTimestamp(JsonElement element, string propertyName, out DateTimeOffset value)
    {
        value = default;

        if (!element.TryGetProperty(propertyName, out var property))
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
