using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Overworld.Bedrock.Json;
using Overworld.Bedrock.Results;

namespace Overworld.Chest;

[SuppressMessage(
    "Design",
    "CA1001",
    Justification = "The gate never hands out its wait handle, so it holds nothing that needs disposing.")]
public sealed class JsonFile<T>(string path, Func<T> empty)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

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
        await _gate.WaitAsync(cancellationToken);

        try
        {
            await WriteUnguardedAsync(value, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<Result<T>> UpdateAsync(Func<T, Result<T>> change, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var current = await ReadAsync(cancellationToken);

            if (!current.IsSuccess)
            {
                return current;
            }

            var changed = change(current.Value);

            if (changed.IsSuccess && !ReferenceEquals(changed.Value, current.Value))
            {
                await WriteUnguardedAsync(changed.Value, cancellationToken);
            }

            return changed;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task WriteUnguardedAsync(T value, CancellationToken cancellationToken)
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
