using Overworld.Apps.Crafter.Projects;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Summaries;

[Operation("show-summary", "Shows a project's summary: what it is, its stack, its layout and its commands, in the user's words.")]
public sealed class ShowSummaryOp(Shelf shelf, SummaryStore store) : IOperation<ShowSummary, Summary>
{
    public async Task<Result<Summary>> ExecuteAsync(ShowSummary input, CancellationToken cancellationToken)
    {
        var found = await shelf.FindAsync(input.Folder, cancellationToken);

        return found.IsSuccess ? await store.ReadAsync(found.Value, cancellationToken) : found.Error;
    }
}
