using Overworld.Apps.Crafter.Summaries;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Proposals;

[Operation("propose-summary", "Proposes a whole new summary for the project; the user approves or declines it in the chat, and an approved one replaces the summary.")]
public sealed class ProposeSummaryOp(SummaryStore summaries, Asker ask, Action? kept = null) : IOperation<ProposeSummary, string>
{
    public async Task<Result<string>> ExecuteAsync(ProposeSummary input, CancellationToken cancellationToken)
    {
        var proposed = input.Summary.Trim();

        if (proposed.Length == 0)
        {
            return new Error("summary.empty", "A summary needs words.");
        }

        var current = await summaries.ReadAsync(input.Folder, cancellationToken);

        if (!current.IsSuccess)
        {
            return current.Error;
        }

        if (!await ask(new SummaryProposal(current.Value.Text, proposed), cancellationToken))
        {
            return "The user declined the summary.";
        }

        var written = await summaries.WriteAsync(input.Folder, proposed, cancellationToken);

        if (!written.IsSuccess)
        {
            return written.Error;
        }

        kept?.Invoke();

        return "The user approved the summary; the project keeps it now.";
    }
}
