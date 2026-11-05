using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Redstone;

internal sealed partial class Dispatcher(IEnumerable<Operation> operations, ILogger<Dispatcher> logger)
{
    private readonly Catalog _catalog = new(operations);

    public async Task<Response> DispatchAsync(Request request, CancellationToken token)
    {
        if (_catalog.Find(request.Operation) is not { } operation)
            return Response.Failure(new Error("operation.unknown", $"There is no operation '{request.Operation}'."));

        try
        {
            var result = await operation.RunAsync(request.Input ?? new JsonObject(), token);

            return result.IsSuccess
                ? Response.Success(result.Value)
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
