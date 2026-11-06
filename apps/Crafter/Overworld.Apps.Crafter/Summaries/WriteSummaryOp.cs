using Overworld.Apps.Crafter.Projects;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Summaries;

[Operation("write-summary", "Replaces a project's summary, kept as .crafter/summary.md in the project; a blank one takes it away.")]
public sealed class WriteSummaryOp(Shelf shelf, SummaryStore store) : IOperation<WriteSummary, Summary>
{
    public async Task<Result<Summary>> ExecuteAsync(WriteSummary input, CancellationToken cancellationToken)
    {
        var found = await shelf.FindAsync(input.Folder, cancellationToken);

        if (!found.IsSuccess)
        {
            return found.Error;
        }

        var current = await store.ReadAsync(found.Value, cancellationToken);

        if (current.IsSuccess && current.Value.Text == (input.Text.Trim() is { Length: > 0 } text ? text : null))
        {
            return current;
        }

        return await store.WriteAsync(found.Value, input.Text, cancellationToken);
    }
}
