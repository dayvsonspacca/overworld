using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Bell;

[Operation(Name, "Lists the notices waiting on you, oldest first.")]
public sealed class ListNoticesOp(NoticeBoard board) : IOperation<ListNotices, IReadOnlyList<Notice>>
{
    internal const string Name = "bell-list";

    public Task<Result<IReadOnlyList<Notice>>> ExecuteAsync(ListNotices input, CancellationToken cancellationToken) =>
        board.ListAsync(cancellationToken);
}
