using System.Text.Json.Nodes;
using Overworld.Allay.Briefs;

namespace Overworld.Allay.ClaudeCode.Cli;

internal static class McpConfig
{
    public static string Of(IReadOnlyList<ToolServer> servers)
    {
        var configured = new JsonObject();

        foreach (var server in servers)
        {
            var entry = new JsonObject
            {
                ["type"] = "http",
                ["url"] = server.Endpoint.ToString(),
                ["headers"] = new JsonObject { ["Authorization"] = $"Bearer {server.Token}" },
            };

            if (server.Patience is { } patience)
            {
                entry["timeout"] = (long)patience.TotalMilliseconds;
            }

            configured[server.Name] = entry;
        }

        return new JsonObject { ["mcpServers"] = configured }.ToJsonString();
    }
}
