using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Overworld.Chest;

namespace Overworld.Allay.Remote;

public sealed partial class Transcripts(string folder)
{
    private readonly ConcurrentDictionary<string, JsonFile<Transcript?>> _files = new();

    public async Task<Transcript?> LoadAsync(string chat, CancellationToken cancellationToken)
    {
        var read = await FileOf(chat).ReadAsync(cancellationToken);

        return read.IsSuccess ? read.Value : null;
    }

    public Task SaveAsync(string chat, Transcript transcript, CancellationToken cancellationToken) =>
        FileOf(chat).WriteAsync(transcript, cancellationToken);

    public void Forget(string chat)
    {
        try
        {
            File.Delete(PathOf(chat));
        }
        catch (Exception gone) when (gone is IOException or UnauthorizedAccessException)
        {
        }
    }

    private JsonFile<Transcript?> FileOf(string chat) => _files.GetOrAdd(chat, key => new JsonFile<Transcript?>(PathOf(key), () => null));

    private string PathOf(string chat)
    {
        var name = Safe().IsMatch(chat) ? chat : Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(chat)))[..32];

        return Path.Combine(folder, $"{name}.json");
    }

    [GeneratedRegex("^[A-Za-z0-9-]{1,64}$")]
    private static partial Regex Safe();
}
