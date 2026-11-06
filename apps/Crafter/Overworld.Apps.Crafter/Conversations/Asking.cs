using System.Text.RegularExpressions;
using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Library;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Conversations;

public static partial class Asking
{
    public static Result<Prompt> Of(string draft, string folder, IReadOnlyList<string> skills, IReadOnlyList<Attachment> attached)
    {
        var text = draft.Trim();

        if (text.Length == 0 && attached.Count == 0)
        {
            return new Error("crafter.empty_prompt", "Write something or attach a file before sending.");
        }

        if (SkillCalls.Stranger(text, skills) is { } stranger)
        {
            return new Error("crafter.unknown_command", $"/{stranger} is not one of this project's skills, and Crafter only calls its own.");
        }

        var mentioned = Mention()
            .Matches(text)
            .Select(found => found.Groups["path"].Value.TrimEnd('.', ',', ';', ':', '!', '?', ')'))
            .Where(path => File.Exists(Path.Combine(folder, path)) || Directory.Exists(Path.Combine(folder, path)))
            .Select(path => new FileMention(path));

        return new Prompt(text, [.. mentioned.Concat(attached).Distinct()], SkillCalls.In(text, skills));
    }

    [GeneratedRegex(@"(?:^|\s)@(?<path>\S+)")]
    private static partial Regex Mention();
}
