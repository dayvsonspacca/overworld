using System.Text.Json.Nodes;
using Overworld.Bedrock.Results;
using Overworld.Redstone;

namespace Overworld.Beacon;

public sealed class BeaconClient(IRedstoneClient client)
{
    public async Task<Result<string>> LightAsync(string signal, CancellationToken cancellationToken)
    {
        var request = new Request(LightBeaconOp.Name, new JsonObject { ["signal"] = signal });
        var response = await client.SendAsync(request, cancellationToken);

        return response.Error is { } error ? error : signal;
    }
}
