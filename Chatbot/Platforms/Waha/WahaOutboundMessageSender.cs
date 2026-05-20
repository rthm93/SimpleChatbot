using Chatbot.Application;
using Chatbot.Domain;

namespace Chatbot.Platforms.Waha;

public sealed class WahaOutboundMessageSender(
    WahaSendTextClient client,
    IWahaAppMessageStore appMessageStore) : IOutboundMessageSender
{
    public async Task<string?> SendTextAsync(ConversationKey conversation, string text, CancellationToken cancellationToken)
    {
        var messageId = await client.SendTextAsync(conversation.ContactId, text, cancellationToken);

        if (!string.IsNullOrWhiteSpace(messageId))
        {
            await appMessageStore.RecordSentMessageIdAsync(conversation, messageId, cancellationToken);
        }

        return messageId;
    }
}
