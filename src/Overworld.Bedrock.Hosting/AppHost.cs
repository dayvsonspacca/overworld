using Avalonia.Controls;
using Overworld.Bedrock.Apps;
using Overworld.Bedrock.Cli;
using Overworld.Bedrock.Desktop;
using Overworld.Bedrock.Mcp;
using Overworld.Bedrock.Operations;

namespace Overworld.Bedrock.Hosting;

public static class AppHost
{
    public static async Task<int> RunAsync(
        AppIdentity app,
        IReadOnlyList<Operation> operations,
        string[] args,
        Func<Control>? window = null)
    {
        if (operations.Any(operation => operation.Name == "mcp"))
        {
            throw new ArgumentException("No operation may be named 'mcp'; that name starts the MCP server.", nameof(operations));
        }

        if (args is [] && window is not null)
        {
            return DesktopHost.Run(app, window, args);
        }

        if (args is ["mcp"])
        {
            await new McpHost(app.Command, operations)
                .RunAsync(Console.OpenStandardInput(), Console.OpenStandardOutput(), CancellationToken.None);

            return 0;
        }

        return await new CliHost(app.Command, operations)
            .RunAsync(args, Console.Out, Console.Error, CancellationToken.None);
    }
}
