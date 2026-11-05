using System.Text.Json;
using Overworld.Bedrock.Json;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Redstone;

public sealed class RemoteOperation(Operation operation, IRedstoneClient client)
    : Operation(operation.Name, operation.Description, operation.InputType)
{
    public override async Task<Result<object?>> ExecuteAsync(object input, CancellationToken cancellationToken)
    {
        var request = new Request(Name, JsonSerializer.SerializeToNode(input, InputType, BedrockJson.Options));
        var response = await client.SendAsync(request, cancellationToken);

        return response.Error is { } error
            ? Result.Failure<object?>(error)
            : Result.Success<object?>(response.Result);
    }
}
