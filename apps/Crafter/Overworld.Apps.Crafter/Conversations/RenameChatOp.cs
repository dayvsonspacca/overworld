using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Conversations;

[Operation("rename-chat", "Gives this chat a new title, the one Crafter lists it by.")]
public sealed class RenameChatOp(Action<ChatTitle> retitle) : IOperation<RenameChat, string>
{
    public Task<Result<string>> ExecuteAsync(RenameChat input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input.Title))
        {
            return Task.FromResult<Result<string>>(new Error("chat.title_empty", "A chat's title needs words."));
        }

        var title = ChatTitle.Of(input.Title);
        retitle(title);

        return Task.FromResult(Result.Success($"The chat is titled \"{title.Text}\" now."));
    }
}
