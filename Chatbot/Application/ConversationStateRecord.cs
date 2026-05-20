using Chatbot.Domain;

namespace Chatbot.Application;

public sealed record ConversationStateRecord(ConversationState State, DateTimeOffset UpdatedAt);
