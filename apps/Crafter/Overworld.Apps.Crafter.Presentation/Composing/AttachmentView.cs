using Overworld.Allay.Conversations;

namespace Overworld.Apps.Crafter.Presentation.Composing;

public sealed record AttachmentView(Attachment Attachment)
{
    public bool IsPicture => Attachment is Picture;

    public byte[]? Data => (Attachment as Picture)?.Data;

    public string Label => Attachment switch
    {
        FileMention file => Path.GetFileName(file.Path.TrimEnd('/')) is { Length: > 0 } name ? name : file.Path,
        Picture picture => picture.MediaType,
        _ => string.Empty,
    };

    public string Tip => Attachment is FileMention file ? file.Path : "Picture";
}
