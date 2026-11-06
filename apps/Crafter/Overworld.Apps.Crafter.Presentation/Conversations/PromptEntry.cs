using Overworld.Allay.Conversations;

namespace Overworld.Apps.Crafter.Presentation.Conversations;

public sealed class PromptEntry(Prompt prompt) : Entry
{
    public string Text { get; } = prompt.Text;

    public bool HasText => Text.Trim().Length > 0;

    public IReadOnlyList<string> Files { get; } = [.. prompt.Attachments.OfType<FileMention>().Select(file => file.Path)];

    public IReadOnlyList<PictureView> Pictures { get; } = [.. prompt.Attachments.OfType<Picture>().Select(picture => new PictureView(picture, "Pasted by you"))];

    public IReadOnlyList<string> Skills { get; } = prompt.Skills;

    public bool HasFiles => Files.Count > 0;

    public bool HasSkills => Skills.Count > 0;

    public bool HasPictures => Pictures.Count > 0;
}
