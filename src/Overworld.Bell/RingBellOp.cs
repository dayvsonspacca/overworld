using Overworld.Beacon;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Bell;

[Operation(Name, "Rings the bell: shows a notice that may ask something, and keeps it until it is answered or withdrawn.")]
public sealed class RingBellOp(NoticeBoard board, Beam beam, TimeProvider time) : IOperation<RingBell, Notice>
{
    internal const string Name = "bell-ring";

    public async Task<Result<Notice>> ExecuteAsync(RingBell input, CancellationToken cancellationToken)
    {
        var from = input.From.Trim();
        var title = input.Title.Trim();

        if (from.Length == 0)
            return new Error("bell.from_empty", "A notice needs the app that rings it, like crafter.");

        if (title.Length == 0)
            return new Error("bell.title_empty", "A notice needs a title.");

        var notice = new Notice(
            Guid.NewGuid().ToString("N"),
            from,
            title,
            input.Body,
            input.Context,
            input.Color,
            input.Ask,
            input.Quiet,
            time.GetUtcNow());

        var rung = await board.RingAsync(notice, cancellationToken);
        if (rung.IsSuccess)
            beam.Light(Signal.Name);

        return rung;
    }
}
