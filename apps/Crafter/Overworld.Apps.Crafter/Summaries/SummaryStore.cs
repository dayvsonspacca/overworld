using System.Collections.Concurrent;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Summaries;

public sealed class SummaryStore
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates = new();

    public async Task<Result<Summary>> ReadAsync(string folder, CancellationToken cancellationToken)
    {
        var file = FileIn(folder);
        var gate = GateOf(folder);

        await gate.WaitAsync(cancellationToken);

        try
        {
            return File.Exists(file) && (await File.ReadAllTextAsync(file, cancellationToken)).Trim() is { Length: > 0 } text
                ? new Summary(text)
                : new Summary(null);
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        {
            return new Error("file.unreadable", $"{file} could not be read: {failure.Message}");
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<Result<Summary>> WriteAsync(string folder, string text, CancellationToken cancellationToken)
    {
        var file = FileIn(folder);
        var trimmed = text.Trim();
        var gate = GateOf(folder);

        await gate.WaitAsync(cancellationToken);

        try
        {
            if (trimmed.Length == 0)
            {
                File.Delete(file);

                return new Summary(null);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(file)!);

            var temporary = file + ".tmp";

            await File.WriteAllTextAsync(temporary, trimmed + "\n", cancellationToken);
            File.Move(temporary, file, overwrite: true);

            return new Summary(trimmed);
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        {
            return new Error("file.unwritable", $"{file} could not be written: {failure.Message}");
        }
        finally
        {
            gate.Release();
        }
    }

    private static string FileIn(string folder) => Path.Combine(folder, ".crafter", "summary.md");

    private SemaphoreSlim GateOf(string folder) => _gates.GetOrAdd(folder, _ => new SemaphoreSlim(1, 1));
}
