using Overworld.Apps.Crafter.Projects;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Conversations;

[Operation("open-chat", "Opens a new chat in a project open in Crafter, in Crafter's window, and sends it its first message if one is given; the project's agent takes it from there.")]
public sealed class OpenChatOp(Shelf shelf, Func<string, string?, Task<Result<ChatId>>> open) : IOperation<OpenChat, string>
{
    public async Task<Result<string>> ExecuteAsync(OpenChat input, CancellationToken cancellationToken)
    {
        var found = await shelf.FindAsync(input.Folder, cancellationToken);

        if (!found.IsSuccess)
        {
            return found.Error;
        }

        var prompt = string.IsNullOrWhiteSpace(input.Prompt) ? null : input.Prompt.Trim();
        var opened = await open(found.Value, prompt);

        if (!opened.IsSuccess)
        {
            return opened.Error;
        }

        return prompt is null
            ? $"Crafter opened chat {opened.Value.Value} in {found.Value}; it waits for the user's first message."
            : $"Crafter opened chat {opened.Value.Value} in {found.Value} and sent it the prompt; the project's agent is working on it.";
    }
}
