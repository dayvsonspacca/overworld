using Overworld.Beacon;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Bell;

[Operation(Name, "Withdraws a notice that no longer needs you, like a question answered in the app itself.")]
public sealed class WithdrawNoticeOp(NoticeBoard board, Beam beam) : IOperation<WithdrawNotice, Notice>
{
    internal const string Name = "bell-withdraw";

    public async Task<Result<Notice>> ExecuteAsync(WithdrawNotice input, CancellationToken cancellationToken)
    {
        var withdrawn = await board.WithdrawAsync(input.Id, cancellationToken);
        if (withdrawn.IsSuccess)
            beam.Light(Signal.Name);

        return withdrawn;
    }
}
