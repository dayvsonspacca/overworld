using Overworld.Apps.Crafter.Axioms;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Proposals;

[Operation("propose-axiom-removal", "Proposes dropping one of the project's axioms that no longer holds; the user approves or declines it in the chat, and an approved one leaves the project's axioms.")]
public sealed class ProposeAxiomRemovalOp(AxiomBook book, Asker ask, Action? kept = null) : IOperation<ProposeAxiomRemoval, string>
{
    public async Task<Result<string>> ExecuteAsync(ProposeAxiomRemoval input, CancellationToken cancellationToken)
    {
        var axiom = input.Axiom.Trim();
        var held = await book.ListAsync(input.Folder, cancellationToken);

        if (!held.IsSuccess)
        {
            return held.Error;
        }

        if (!held.Value.Contains(axiom))
        {
            return new Error("axiom.missing", "The project holds no such axiom; propose dropping one word for word as it is held.");
        }

        if (!await ask(new AxiomRemovalProposal(axiom, input.Why.Trim()), cancellationToken))
        {
            return "The user declined; the axiom stays.";
        }

        var removed = await new RemoveAxiomOp(book).ExecuteAsync(new RemoveAxiom(input.Folder, axiom), cancellationToken);

        if (!removed.IsSuccess)
        {
            return removed.Error;
        }

        kept?.Invoke();

        return "The user approved; the project no longer holds the axiom.";
    }
}
