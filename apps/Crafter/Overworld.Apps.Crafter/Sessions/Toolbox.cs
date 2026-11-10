using System.Text.Json.Nodes;
using Overworld.Allay.Briefs;
using Overworld.Apps.Crafter.Axioms;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Proposals;
using Overworld.Apps.Crafter.Summaries;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;
using Overworld.Mcp;

namespace Overworld.Apps.Crafter.Sessions;

public sealed class Toolbox(McpHttpHost host, IReadOnlyList<Operation> reads, IReadOnlyList<Operation> writes, AxiomBook axioms, SummaryStore summaries, string? command = null)
{
    public const string Name = "crafter";

    public const string Live = "crafter-chat";

    public const string Serve = "tools";

    public const string Signal = "crafter";

    public static readonly TimeSpan Patience = TimeSpan.FromMinutes(30);

    private const string Instructions =
        "Crafter, the app this session runs in, keeps this project's todos, each with a number; the user may cite one in a message as \"Todo #3: …\". "
            + "Read them with list-todos; when you finish one, mark it with complete-todo. Add one with add-todo when the user asks you to note something for later, "
            + "and remove one with remove-todo when the user drops it. They are always this project's; you never name the project. "
            + "Crafter keeps the project's other chats too: list-chats lists them and read-chat reads one, when the user refers to something talked about before. "
            + "Rename or give the project an icon with edit-project when the user asks. "
            + "Crafter keeps a library of skills: show-library lists them with the folder of each, whose instructions.md holds the skill; "
            + "save-skill writes one when the user asks to keep a way of working as a skill, and give-skill turns one on or off for the project. "
            + "To show the user a picture, like a screenshot you took, a mockup you rendered or an image you made, call show-picture with its file; it appears in the chat. "
            + "To show the user a page, like a mockup, a chart, a palette's swatches or a table too wide for words, call paint with its HTML and a title; "
            + "Painting draws it right in the chat and keeps it. Paint again with the id paint gave to make the next version of that same page instead of painting a new one; "
            + "each version shows up as it is painted and the earlier ones stay. Give group Design to a page that designs a screen, a component or an app. "
            + "painting-list lists the paintings, painting-show shows one version of one, painting-restore goes back to an earlier version by painting it again as the newest, and painting-group puts a painting in a group without a new version. "
            + "Dye keeps the user's color palettes and Overworld's themes: dye-list and dye-themes-list list them, and dye-show shows a palette and dye-themes-show a theme right in the chat, with its colors.";

    private const string Naming =
        " Once this chat's subject is clear, give it a short title with rename-chat, a few words like \"Pictures in the conversation\", and rename it when the subject changes.";

    private const string Proposing =
        " Crafter also keeps this project's axioms, the rules you follow in every chat, and its summary: what the project is, its stack, its layout and the commands it runs with. "
            + "When the user corrects you or settles how something must always be done here, propose it as an axiom with propose-axiom, short and in the imperative, like \"Never add comments\". "
            + "Don't propose what is already an axiom, nor what only holds for the task at hand. "
            + "When an axiom no longer holds, because the user said otherwise or the project moved on, propose dropping it with propose-axiom-removal and say why. "
            + "When you learn something lasting about the project that the summary lacks or gets wrong, propose the whole new summary with propose-summary. "
            + "The user approves or declines each proposal in the chat.";

    public IReadOnlyList<ToolServer> Admit(string folder, Asker? ask = null, Action? kept = null, Action<ChatTitle>? retitle = null)
    {
        Dictionary<string, JsonNode> presets = new() { ["folder"] = folder };
        Operation[] shared = [.. reads, .. kept is null ? writes : writes.Select(Operation (write) => new Keeping(write, kept))];
        Operation[] live =
        [
            .. command is null ? shared : [],
            .. retitle is null ? [] : (Operation[])[Operation.From(new RenameChatOp(retitle))],
            .. ask is null
                ? []
                : (Operation[])[Operation.From(new ProposeAxiomOp(axioms, ask, kept)), Operation.From(new ProposeAxiomRemovalOp(axioms, ask, kept)), Operation.From(new ProposeSummaryOp(summaries, ask, kept))],
        ];
        List<ToolServer> servers = [];

        if (command is { } started)
        {
            servers.Add(ToolServer.Started(Name, started, [Serve, folder], [.. reads.Concat(writes).Select(operation => operation.Name)], Patience));
        }

        if (live.Length > 0 || command is null)
        {
            var instructions = (command is null ? Instructions : string.Empty) + (retitle is null ? string.Empty : Naming) + (ask is null ? string.Empty : Proposing);
            var token = host.Admit(new Caller(folder, [.. live.Select(operation => new ServedTool(operation, presets))], instructions.Trim()));

            servers.Add(new ToolServer(
                command is null ? Name : Live,
                host.Endpoint ?? throw new InvalidOperationException("The tool server has not started."),
                token,
                [.. live.Select(operation => operation.Name)],
                Patience));
        }

        return servers;
    }

    public ToolServer Lend(string name, IReadOnlyList<Operation> operations, string instructions)
    {
        var token = host.Admit(new Caller(name, [.. operations.Select(operation => new ServedTool(operation))], instructions));

        return new ToolServer(name, host.Endpoint ?? throw new InvalidOperationException("The tool server has not started."), token, [], Patience);
    }

    public void Release(IEnumerable<ToolServer> servers)
    {
        foreach (var server in servers)
        {
            if (server.Token is { } token)
            {
                host.Release(token);
            }
        }
    }

    public Task ServeAsync(string folder, Action kept, CancellationToken cancellationToken)
    {
        Dictionary<string, JsonNode> presets = new() { ["folder"] = folder };
        Operation[] shared = [.. reads, .. writes.Select(Operation (write) => new Keeping(write, kept))];

        return new McpHost(Name, [.. shared.Select(operation => new ServedTool(operation, presets))], Instructions)
            .RunAsync(Console.OpenStandardInput(), Console.OpenStandardOutput(), cancellationToken);
    }

    public static Operation Borrow(string app, Operation operation) => new Borrowed($"{app}-{operation.Name}", operation);

    private sealed class Borrowed(string name, Operation operation) : Operation(name, operation.Description, operation.InputType)
    {
        public override Task<Result<object?>> ExecuteAsync(object input, CancellationToken cancellationToken) =>
            operation.ExecuteAsync(input, cancellationToken);
    }

    private sealed class Keeping(Operation write, Action kept) : Operation(write.Name, write.Description, write.InputType)
    {
        public override async Task<Result<object?>> ExecuteAsync(object input, CancellationToken cancellationToken)
        {
            var done = await write.ExecuteAsync(input, cancellationToken);

            if (done.IsSuccess)
            {
                kept();
            }

            return done;
        }
    }
}
