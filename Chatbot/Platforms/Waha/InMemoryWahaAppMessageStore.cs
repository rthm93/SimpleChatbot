using System.Collections.Concurrent;
using Chatbot.Domain;

namespace Chatbot.Platforms.Waha;

public sealed class InMemoryWahaAppMessageStore : IWahaAppMessageStore
{
    private readonly ConcurrentDictionary<string, ConversationKey> _sentIds = new(StringComparer.Ordinal);

    public ValueTask RecordSentMessageIdAsync(ConversationKey conversation, string messageId, CancellationToken cancellationToken)
    {
        _sentIds[messageId] = conversation;
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> ContainsSentMessageIdAsync(string messageId, CancellationToken cancellationToken) =>
        ValueTask.FromResult(_sentIds.ContainsKey(messageId));
}
