using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Axioms;

[Operation("remove-axiom", "Drops one of a project's axioms and lists the ones left.")]
public sealed class RemoveAxiomOp(AxiomBook book) : IOperation<RemoveAxiom, IReadOnlyList<string>>
{
    public Task<Result<IReadOnlyList<string>>> ExecuteAsync(RemoveAxiom input, CancellationToken cancellationToken)
    {
        var axiom = input.Axiom.Trim();

        return book.ChangeAsync(
            input.Folder,
            held => held.Contains(axiom)
                ? Result.Success<IReadOnlyList<string>>([.. held.Where(each => each != axiom)])
                : new Error("axiom.missing", "The project holds no such axiom."),
            cancellationToken);
    }
}
