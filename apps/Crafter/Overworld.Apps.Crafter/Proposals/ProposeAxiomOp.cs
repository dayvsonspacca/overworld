using Overworld.Apps.Crafter.Axioms;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Proposals;

[Operation("propose-axiom", "Proposes an axiom for the project; the user approves or declines it in the chat, and an approved one joins the project's axioms.")]
public sealed class ProposeAxiomOp(AxiomBook book, Asker ask, Action? kept = null) : IOperation<ProposeAxiom, string>
{
    public async Task<Result<string>> ExecuteAsync(ProposeAxiom input, CancellationToken cancellationToken)
    {
        var axiom = input.Axiom.Trim();

        if (axiom.Length == 0)
        {
            return new Error("axiom.empty", "An axiom needs words.");
        }

        var held = await book.ListAsync(input.Folder, cancellationToken);

        if (!held.IsSuccess)
        {
            return held.Error;
        }

        if (held.Value.Contains(axiom))
        {
            return new Error("axiom.held", "The project already holds that axiom.");
        }

        if (!await ask(new AxiomProposal(axiom), cancellationToken))
        {
            return "The user declined the axiom.";
        }

        var added = await new AddAxiomOp(book).ExecuteAsync(new AddAxiom(input.Folder, axiom), cancellationToken);

        if (!added.IsSuccess)
        {
            return added.Error;
        }

        kept?.Invoke();

        return "The user approved the axiom; the project holds it now.";
    }
}
