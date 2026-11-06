using Overworld.Allay.Briefs;
using Overworld.Allay.ClaudeCode.Cli;

namespace Overworld.Allay.ClaudeCode.Tests;

internal sealed class FakeClaude : IDisposable
{
    public const string Init = """echo '{"type":"system","subtype":"init","session_id":"s-1","model":"claude-opus-5-5"}'""";

    public const string Success = """echo '{"type":"result","subtype":"success","is_error":false,"stop_reason":"end_turn","session_id":"s-1"}'""";

    public const string SavesArguments = "printf '%s\\n' \"$@\" > \"$(dirname \"$0\")/arguments\"";

    public FakeClaude(string script)
    {
        Folder = Directory.CreateTempSubdirectory("overworld-fake-claude-").FullName;
        Executable = Path.Combine(Folder, "claude");
        File.WriteAllText(Executable, $"#!/usr/bin/env bash\n{script}\n");

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(Executable, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    public string Folder { get; }

    public string Executable { get; }

    public string[] Arguments => File.ReadAllLines(Path.Combine(Folder, "arguments"));

    public Brief Brief(string? context = null, IReadOnlyList<string>? rules = null, IReadOnlyList<Skill>? skills = null, IReadOnlyList<ToolServer>? tools = null) =>
        new(Folder, context, rules ?? [], skills ?? [], tools ?? []);

    public static string Writes(string text) =>
        """printf '{"type":"stream_event","event":{"type":"content_block_delta","delta":{"type":"text_delta","text":"%s"}}}\n' """
        + $"\"{text}\"";

    public Session Session(Brief? brief = null, string? resume = null, Capabilities? capabilities = null) =>
        new(Executable, () => capabilities ?? Capabilities.Unknown, Path.Combine(Folder, "shelf"), brief ?? Brief(), resume);

    public void Dispose() => Directory.Delete(Folder, recursive: true);
}
