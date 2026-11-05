using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Overworld.Bedrock.Json;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Redstone;

internal sealed partial class Dispatcher(IEnumerable<Operation> operations, ILogger<Dispatcher> logger)
{
    private readonly Dictionary<string, Operation> _operations = operations.ToDictionary(operation => operation.Name);

    public async Task<Response> DispatchAsync(Request request, CancellationToken token)
    {
        if (!_operations.TryGetValue(request.Operation, out var operation))
            return Response.Failure(new Error("operation.unknown", $"There is no operation '{request.Operation}'."));

        var input = BedrockJson.ReadInput(request.Input ?? new JsonObject(), operation.InputType);
        if (!input.IsSuccess)
            return Response.Failure(input.Error);

        try
        {
            var result = await operation.ExecuteAsync(input.Value, token);

            return result.IsSuccess
                ? Response.Success(JsonSerializer.SerializeToNode(result.Value, BedrockJson.Options))
                : Response.Failure(result.Error);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogOperationCrashed(exception, request.Operation);
            return Response.Failure(new Error("operation.crashed", $"'{request.Operation}' failed unexpectedly; the daemon's log has the details."));
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Operation {Operation} crashed")]
    private partial void LogOperationCrashed(Exception exception, string operation);
}
