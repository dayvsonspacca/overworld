using System.Reflection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Overworld.Bedrock.Operations;

namespace Overworld.Mcp;

public sealed class McpHost(string name, IEnumerable<Operation> operations)
{
    public async Task RunAsync(Stream input, Stream output, CancellationToken cancellationToken)
    {
        var options = new McpServerOptions
        {
            ServerInfo = new Implementation
            {
                Name = name,
                Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0",
            },
            ToolCollection = [.. new Catalog(operations).All.Select(operation => new OperationTool(operation))],
        };

        await using var transport = new StreamServerTransport(input, output, name);
        await using var server = McpServer.Create(transport, options);

        await server.RunAsync(cancellationToken);
    }
}
