using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Bell;

[Operation(Name, "Hands an app the notices it rang that were answered or asked to be opened since it last collected.")]
public sealed class CollectNoticesOp(NoticeBoard board) : IOperation<CollectNotices, IReadOnlyList<Notice>>
{
    internal const string Name = "bell-collect";

    public Task<Result<IReadOnlyList<Notice>>> ExecuteAsync(CollectNotices input, CancellationToken cancellationToken) =>
        board.CollectAsync(input.From.Trim(), cancellationToken);
}
