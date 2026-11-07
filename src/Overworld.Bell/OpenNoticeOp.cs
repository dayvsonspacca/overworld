using Overworld.Beacon;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Bell;

[Operation(Name, "Asks the app that rang a notice to show where it comes from; the notice keeps waiting.")]
public sealed class OpenNoticeOp(NoticeBoard board, Beam beam) : IOperation<OpenNotice, Notice>
{
    internal const string Name = "bell-open";

    public async Task<Result<Notice>> ExecuteAsync(OpenNotice input, CancellationToken cancellationToken)
    {
        var opened = await board.OpenAsync(input.Id, cancellationToken);
        if (opened.IsSuccess)
            beam.Light(Signal.Name);

        return opened;
    }
}
