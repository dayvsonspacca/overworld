using System.Text.Json;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Presentation.Workbench;
using Overworld.Redstone;
using Overworld.Web.Serving;

namespace Overworld.Apps.Crafter.Desktop.Summoning;

public sealed class Presence : IDisposable
{
    public const string Command = "summon";

    private static readonly string Name = Environment.GetEnvironmentVariable(DevServer.Folder) is null ? "crafter" : "crafter-dev";

    private static readonly string Lock = Path.ChangeExtension(SocketPath.Resolve($"overworld-{Name}"), ".lock");

    private static readonly string Waiting = Path.ChangeExtension(SocketPath.Resolve($"overworld-{Name}"), ".summons");

    public static string Signal { get; } = $"{Name}-summon";

    private readonly FileStream _held;

    private Presence(FileStream held) => _held = held;

    public static Presence? Claim()
    {
        try
        {
            return new Presence(new FileStream(Lock, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
        }
        catch (IOException)
        {
            return null;
        }
    }

    public static void Leave(Summons summons) =>
        File.WriteAllText(Waiting, JsonSerializer.Serialize(new Left(summons.Folder, summons.Chat.Value)));

    public static Summons? Read(IReadOnlyList<string> args) =>
        args is [Command, var folder, var chat] && Guid.TryParse(chat, out var id) ? new Summons(folder, new ChatId(id)) : null;

    public static IReadOnlyList<string>? Opener(string? command, string folder, ChatId chat) =>
        command is null ? null : [command, Command, folder, chat.Value.ToString()];

    public static Summons? Take()
    {
        try
        {
            var left = JsonSerializer.Deserialize<Left>(File.ReadAllText(Waiting));
            File.Delete(Waiting);

            return left is null ? null : new Summons(left.Folder, new ChatId(left.Chat));
        }
        catch (Exception missing) when (missing is IOException or JsonException)
        {
            return null;
        }
    }

    public void Dispose() => _held.Dispose();

    private sealed record Left(string Folder, Guid Chat);
}
