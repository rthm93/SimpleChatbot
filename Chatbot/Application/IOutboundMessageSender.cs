using Chatbot.Domain;

namespace Chatbot.Application;

public interface IOutboundMessageSender
{
    Task<string?> SendTextAsync(ConversationKey conversation, string text, CancellationToken cancellationToken);
}
