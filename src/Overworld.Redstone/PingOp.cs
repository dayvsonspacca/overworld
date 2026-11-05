using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Redstone;

[Operation("ping", "Answers pong while the daemon is running.")]
public sealed class PingOp : IOperation<Ping, string>
{
    public Task<Result<string>> ExecuteAsync(Ping input, CancellationToken cancellationToken) =>
        Task.FromResult<Result<string>>("pong");
}
