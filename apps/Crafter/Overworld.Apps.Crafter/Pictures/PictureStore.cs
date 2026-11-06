using System.Security.Cryptography;
using Overworld.Allay.Conversations;

namespace Overworld.Apps.Crafter.Pictures;

public sealed class PictureStore(string directory)
{
    private static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/gif"] = ".gif",
        ["image/webp"] = ".webp",
    };

    public static string? MediaTypeOf(string file) =>
        Path.GetExtension(file).ToLowerInvariant() switch
        {
            ".jpeg" => "image/jpeg",
            var extension => Extensions.FirstOrDefault(each => each.Value == extension).Key,
        };

    public string? Keep(Picture picture)
    {
        if (!Extensions.TryGetValue(picture.MediaType, out var extension))
        {
            return null;
        }

        var name = Convert.ToHexStringLower(SHA256.HashData(picture.Data))[..16] + extension;
        var file = PathTo(name);

        if (!File.Exists(file))
        {
            Directory.CreateDirectory(directory);
            var fresh = file + ".new";
            File.WriteAllBytes(fresh, picture.Data);
            File.Move(fresh, file, overwrite: true);
        }

        return name;
    }

    public Picture? Find(string name)
    {
        var file = PathTo(name);

        return MediaTypeOf(file) is { } mediaType && File.Exists(file)
            ? new Picture(mediaType, File.ReadAllBytes(file))
            : null;
    }

    public string PathTo(string name) => Path.Combine(directory, Path.GetFileName(name));
}
