using System.Text.Json;

namespace Chatbot.Domain;

public sealed record NormalizedMessage(
    ConversationKey Conversation,
    MessageDirection Direction,
    string Text,
    DateTimeOffset Timestamp,
    string? ExternalMessageId,
    JsonElement RawPayload);
