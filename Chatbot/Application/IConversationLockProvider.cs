using Chatbot.Domain;

namespace Chatbot.Application;

public interface IConversationLockProvider
{
    ValueTask<IAsyncDisposable> AcquireAsync(ConversationKey key, CancellationToken cancellationToken);
}
