using System.Text;
using Overworld.Allay.Conversations;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Conversations;

[Operation("read-chat", "Reads one of a project's chats in Crafter: what the user asked, what the agent said and the steps it took, in order.")]
public sealed class ReadChatOp(ChatStore store) : IOperation<ReadChat, string>
{
    public async Task<Result<string>> ExecuteAsync(ReadChat input, CancellationToken cancellationToken)
    {
        var chats = await store.LoadAsync(input.Folder, cancellationToken);

        if (!chats.IsSuccess)
        {
            return chats.Error;
        }

        if (chats.Value.FirstOrDefault(chat => chat.Id.Value == input.Chat) is not { } found)
        {
            return new Error("chat.not_found", $"The project has no chat {input.Chat}; list-chats gives the ids it has.");
        }

        var told = new StringBuilder($"# {found.Title.Text}");

        foreach (var turn in found.Turns)
        {
            told.Append(turn switch
            {
                Prompt prompt => $"\n\n## User\n\n{prompt.Text}",
                Reply reply => $"\n\n## Agent\n\n{string.Concat(reply.Moments.Select(Told))}".TrimEnd(),
                _ => string.Empty,
            });
        }

        return told.ToString();
    }

    private static string Told(Moment moment) => moment switch
    {
        Speech speech => speech.Text,
        Act act => $"\n- {act.Kind}: {act.Subject}\n",
        _ => string.Empty,
    };
}
