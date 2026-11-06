using Overworld.Allay.Conversations;

namespace Overworld.Apps.Crafter.Presentation.Conversations;

public sealed class PromptEntry(Prompt prompt) : Entry
{
    public string Text { get; } = prompt.Text;

    public IReadOnlyList<string> Files { get; } = [.. prompt.Attachments.OfType<FileMention>().Select(file => file.Path)];

    public int Pictures { get; } = prompt.Attachments.OfType<Picture>().Count();

    public IReadOnlyList<string> Skills { get; } = prompt.Skills;

    public bool HasFiles => Files.Count > 0;

    public bool HasSkills => Skills.Count > 0;

    public bool HasPictures => Pictures > 0;

    public string PicturesLabel => Pictures == 1 ? "1 picture" : $"{Pictures} pictures";
}
