using Overworld.Allay.Conversations;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Pictures;

[Operation("show-picture", "Shows the user a picture in the chat, like a screenshot, a mockup or a diagram. Crafter keeps a copy, so it stays in the chat.")]
public sealed class ShowPictureOp(PictureStore store) : IOperation<ShowPicture, PictureShown>
{
    private const long Heaviest = 20_000_000;

    public async Task<Result<PictureShown>> ExecuteAsync(ShowPicture input, CancellationToken cancellationToken)
    {
        var file = Path.GetFullPath(input.File, input.Folder);

        if (PictureStore.MediaTypeOf(file) is not { } mediaType)
        {
            return new Error("picture.unsupported", $"{input.File} is not a PNG, JPEG, GIF or WebP picture.");
        }

        var found = new FileInfo(file);

        if (!found.Exists)
        {
            return new Error("picture.not_found", $"There is no file at {file}.");
        }

        if (found.Length is 0 or > Heaviest)
        {
            return new Error("picture.size", $"{input.File} is empty or heavier than 20 MB.");
        }

        var kept = store.Keep(new Picture(mediaType, await File.ReadAllBytesAsync(file, cancellationToken)));
        var caption = string.IsNullOrWhiteSpace(input.Caption) ? null : input.Caption.Trim();

        return kept is null
            ? new Error("picture.unsupported", $"{input.File} is not a PNG, JPEG, GIF or WebP picture.")
            : new PictureShown(kept, Path.GetFileName(file), caption);
    }
}
