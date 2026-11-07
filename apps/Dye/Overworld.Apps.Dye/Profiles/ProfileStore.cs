using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Profiles;

public sealed class ProfileStore(string user, string picture)
{
    private static readonly HashSet<string> Kinds = new([".png", ".jpg", ".jpeg", ".webp"], StringComparer.OrdinalIgnoreCase);

    public Profile Read() => new(user, File.Exists(picture) ? picture : null);

    public async Task<Result<Profile>> SetPictureAsync(string source, CancellationToken cancellationToken)
    {
        var from = Path.GetFullPath(source);

        if (!File.Exists(from))
        {
            return new Error("profile.picture_not_found", $"There is no picture at {from}.");
        }

        if (!Kinds.Contains(Path.GetExtension(from)))
        {
            return new Error("profile.picture_kind", $"{from} is not a PNG, JPEG or WebP picture.");
        }

        var swap = $"{picture}.{Guid.NewGuid():N}";

        await using (var reading = File.OpenRead(from))
        await using (var writing = File.Create(swap))
        {
            await reading.CopyToAsync(writing, cancellationToken);
        }

        File.Move(swap, picture, overwrite: true);

        return Read();
    }

    public Profile RemovePicture()
    {
        File.Delete(picture);

        return Read();
    }
}
