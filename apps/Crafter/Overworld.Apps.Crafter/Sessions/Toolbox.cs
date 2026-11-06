using System.Text.Json.Nodes;
using Overworld.Allay.Briefs;
using Overworld.Apps.Crafter.Todos;
using Overworld.Bedrock.Operations;
using Overworld.Mcp;

namespace Overworld.Apps.Crafter.Sessions;

public sealed class Toolbox(McpHttpHost host, TodoList todos)
{
    public const string Name = "crafter";

    public static readonly TimeSpan Patience = TimeSpan.FromMinutes(30);

    private const string Instructions =
        "Crafter, the app this session runs in, keeps this project's todos, each with a number; the user may cite one in a message as \"Todo #3: …\". "
            + "Read them with list-todos; when you finish one, mark it with complete-todo. Add one with add-todo when the user asks you to note something for later, "
            + "and remove one with remove-todo when the user drops it. They are always this project's; you never name the project.";

    public ToolServer Admit(string folder)
    {
        Dictionary<string, JsonNode> presets = new() { ["folder"] = folder };
        Operation[] operations =
        [
            Operation.From(new ListTodosOp(todos)),
            Operation.From(new AddTodoOp(todos)),
            Operation.From(new CompleteTodoOp(todos)),
            Operation.From(new RemoveTodoOp(todos)),
        ];

        var token = host.Admit(new Caller(folder, [.. operations.Select(operation => new ServedTool(operation, presets))], Instructions));

        return new ToolServer(
            Name,
            host.Endpoint ?? throw new InvalidOperationException("The tool server has not started."),
            token,
            [.. operations.Select(operation => operation.Name)],
            Patience);
    }

    public void Release(ToolServer server) => host.Release(server.Token);
}
