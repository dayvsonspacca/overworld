using Overworld.Bedrock.Operations;
using Overworld.Cli;
using Overworld.Mcp;

namespace Overworld.Hosting;

public static class AppHost
{
    public static async Task<int> RunAsync(
        string command,
        IReadOnlyList<Operation> operations,
        string[] args,
        Func<Task<int>>? withoutArgs = null)
    {
        if (operations.Any(operation => operation.Name == "mcp"))
        {
            throw new ArgumentException("No operation may be named 'mcp'; that name starts the MCP server.", nameof(operations));
        }

        if (args is [] && withoutArgs is not null)
        {
            return await withoutArgs();
        }

        if (args is ["mcp"])
        {
            await new McpHost(command, operations)
                .RunAsync(Console.OpenStandardInput(), Console.OpenStandardOutput(), CancellationToken.None);

            return 0;
        }

        return await new CliHost(command, operations)
            .RunAsync(args, Console.Out, Console.Error, CancellationToken.None);
    }
}
