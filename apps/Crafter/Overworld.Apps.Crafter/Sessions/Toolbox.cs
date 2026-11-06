using System.Text.Json.Nodes;
using Overworld.Allay.Briefs;
using Overworld.Apps.Crafter.Axioms;
using Overworld.Apps.Crafter.Proposals;
using Overworld.Apps.Crafter.Summaries;
using Overworld.Apps.Crafter.Todos;
using Overworld.Bedrock.Operations;
using Overworld.Mcp;

namespace Overworld.Apps.Crafter.Sessions;

public sealed class Toolbox(McpHttpHost host, TodoList todos, AxiomBook axioms, SummaryStore summaries)
{
    public const string Name = "crafter";

    public static readonly TimeSpan Patience = TimeSpan.FromMinutes(30);

    private const string Instructions =
        "Crafter, the app this session runs in, keeps this project's todos, each with a number; the user may cite one in a message as \"Todo #3: …\". "
            + "Read them with list-todos; when you finish one, mark it with complete-todo. Add one with add-todo when the user asks you to note something for later, "
            + "and remove one with remove-todo when the user drops it. They are always this project's; you never name the project.";

    private const string Proposing =
        " Crafter also keeps this project's axioms, the rules you follow in every chat, and its summary: what the project is, its stack, its layout and the commands it runs with. "
            + "When the user corrects you or settles how something must always be done here, propose it as an axiom with propose-axiom, short and in the imperative, like \"Never add comments\". "
            + "Don't propose what is already an axiom, nor what only holds for the task at hand. "
            + "When you learn something lasting about the project that the summary lacks or gets wrong, propose the whole new summary with propose-summary. "
            + "The user approves or declines each proposal in the chat.";

    public ToolServer Admit(string folder, Asker? ask = null, Action? kept = null)
    {
        Dictionary<string, JsonNode> presets = new() { ["folder"] = folder };
        Operation[] operations =
        [
            Operation.From(new ListTodosOp(todos)),
            Operation.From(new AddTodoOp(todos)),
            Operation.From(new CompleteTodoOp(todos)),
            Operation.From(new RemoveTodoOp(todos)),
            .. ask is null ? [] : (Operation[])[Operation.From(new ProposeAxiomOp(axioms, ask, kept)), Operation.From(new ProposeSummaryOp(summaries, ask, kept))],
        ];

        var token = host.Admit(new Caller(folder, [.. operations.Select(operation => new ServedTool(operation, presets))], ask is null ? Instructions : Instructions + Proposing));

        return new ToolServer(
            Name,
            host.Endpoint ?? throw new InvalidOperationException("The tool server has not started."),
            token,
            [.. operations.Select(operation => operation.Name)],
            Patience);
    }

    public void Release(ToolServer server) => host.Release(server.Token);
}
