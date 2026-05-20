using Chatbot.Domain;

namespace Chatbot.Application;

public interface IConversationStateStore
{
    ValueTask<ConversationStateRecord?> GetAsync(ConversationKey key, CancellationToken cancellationToken);

    ValueTask SetAsync(ConversationKey key, ConversationStateRecord state, CancellationToken cancellationToken);

    ValueTask ClearAsync(ConversationKey key, CancellationToken cancellationToken);
}
