using Overworld.Allay.Conversations;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Conversations;

[Operation("list-chats", "Lists a project's chats in Crafter with their ids, titles and how many prompts each holds.")]
public sealed class ListChatsOp(ChatStore store) : IOperation<ListChats, IReadOnlyList<ChatListing>>
{
    public async Task<Result<IReadOnlyList<ChatListing>>> ExecuteAsync(ListChats input, CancellationToken cancellationToken)
    {
        var chats = await store.LoadAsync(input.Folder, cancellationToken);

        return chats.IsSuccess
            ? Result.Success<IReadOnlyList<ChatListing>>([.. chats.Value.Select(chat => new ChatListing(chat.Id.Value, chat.Title.Text, chat.Turns.OfType<Prompt>().Count()))])
            : chats.Error;
    }
}
