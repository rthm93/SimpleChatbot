using Chatbot.Domain;

namespace Chatbot.Platforms.Waha;

public interface IWahaAppMessageStore
{
    ValueTask RecordSentMessageIdAsync(ConversationKey conversation, string messageId, CancellationToken cancellationToken);

    ValueTask<bool> ContainsSentMessageIdAsync(string messageId, CancellationToken cancellationToken);
}
