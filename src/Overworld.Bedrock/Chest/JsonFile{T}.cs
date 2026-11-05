using System.Text.Json;
using Overworld.Bedrock.Json;
using Overworld.Bedrock.Results;

namespace Overworld.Bedrock.Chest;

public sealed class JsonFile<T>(string path, Func<T> empty)
{
    public async Task<Result<T>> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return empty();
        }

        try
        {
            await using var stream = File.OpenRead(path);

            return await JsonSerializer.DeserializeAsync<T>(stream, BedrockJson.Options, cancellationToken) ?? empty();
        }
        catch (JsonException exception)
        {
            return new Error("file.unreadable", $"{path} is not what it should hold: {exception.Message}");
        }
    }

    public async Task WriteAsync(T value, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);

        var temporary = path + ".tmp";

        await using (var stream = File.Create(temporary))
        {
            await JsonSerializer.SerializeAsync(stream, value, BedrockJson.Options, cancellationToken);
        }

        File.Move(temporary, path, overwrite: true);
    }
}
