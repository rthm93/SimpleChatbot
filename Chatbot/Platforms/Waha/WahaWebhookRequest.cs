using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chatbot.Platforms.Waha;

public sealed record WahaWebhookRequest
{
    public string? Id { get; init; }

    public long? Timestamp { get; init; }

    public string? Event { get; init; }

    public string? Session { get; init; }

    public WahaWebhookMe? Me { get; init; }

    public WahaWebhookPayload? Payload { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record WahaWebhookMe
{
    public string? Id { get; init; }

    public string? PushName { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record WahaWebhookPayload
{
    public JsonElement? Id { get; init; }

    public JsonElement? Timestamp { get; init; }

    public string? From { get; init; }

    public bool? FromMe { get; init; }

    public string? Source { get; init; }

    public string? To { get; init; }

    public string? Body { get; init; }

    public bool? HasMedia { get; init; }

    public JsonElement? Media { get; init; }

    public int? Ack { get; init; }

    public string? AckName { get; init; }

    public JsonElement? Location { get; init; }

    public IReadOnlyList<JsonElement>? VCards { get; init; }

    [JsonPropertyName("_data")]
    public WahaWebhookPayloadData? Data { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record WahaWebhookPayloadData
{
    public WahaWebhookMessageKey? Id { get; init; }

    public string? Body { get; init; }

    public string? Type { get; init; }

    public string? From { get; init; }

    public string? To { get; init; }

    public string? NotifyName { get; init; }

    public bool? Viewed { get; init; }

    public int? Ack { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record WahaWebhookMessageKey
{
    public bool? FromMe { get; init; }

    public string? Remote { get; init; }

    public string? Id { get; init; }

    [JsonPropertyName("_serialized")]
    public string? Serialized { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}
