using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Redstone.Tests;

public sealed record Shout(string Word);

[Operation("echo", "Says the word back, or fails on silence.")]
public sealed class EchoOp : IOperation<Shout, string>
{
    public Task<Result<string>> ExecuteAsync(Shout input, CancellationToken cancellationToken) =>
        Task.FromResult<Result<string>>(input.Word.Length == 0
            ? new Error("echo.silence", "Nothing to echo.")
            : input.Word);
}

[Operation("explode", "Throws, like a creeper.")]
public sealed class ExplodeOp : IOperation<Shout, string>
{
    public Task<Result<string>> ExecuteAsync(Shout input, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Boom.");
}
