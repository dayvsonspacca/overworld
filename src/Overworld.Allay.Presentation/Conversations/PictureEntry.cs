namespace Overworld.Allay.Presentation.Conversations;

public sealed class PictureEntry(PictureView picture, string file, string? caption) : Entry
{
    public PictureView Picture { get; } = picture;

    public string File { get; } = file;

    public string? Caption { get; } = caption;

    public bool HasCaption => Caption is not null;
}
