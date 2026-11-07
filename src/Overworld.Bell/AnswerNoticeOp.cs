using Overworld.Beacon;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Bell;

[Operation(Name, "Answers a notice; the app that rang it collects the answer.")]
public sealed class AnswerNoticeOp(NoticeBoard board, Beam beam) : IOperation<AnswerNotice, Notice>
{
    internal const string Name = "bell-answer";

    public async Task<Result<Notice>> ExecuteAsync(AnswerNotice input, CancellationToken cancellationToken)
    {
        var answered = await board.AnswerAsync(input.Id, input.Reply, cancellationToken);
        if (answered.IsSuccess)
            beam.Light(Signal.Name);

        return answered;
    }
}
