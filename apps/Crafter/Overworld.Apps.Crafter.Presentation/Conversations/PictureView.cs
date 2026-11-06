using Overworld.Allay.Conversations;

namespace Overworld.Apps.Crafter.Presentation.Conversations;

public sealed class PictureView(Picture picture, string label)
{
    public Picture Picture { get; } = picture;

    public byte[] Data => Picture.Data;

    public string Label { get; } = label;
}
