using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Axioms;

[Operation("add-axiom", "Adds an axiom at the end of a project's axioms and lists them all.")]
public sealed class AddAxiomOp(AxiomBook book) : IOperation<AddAxiom, IReadOnlyList<string>>
{
    public Task<Result<IReadOnlyList<string>>> ExecuteAsync(AddAxiom input, CancellationToken cancellationToken)
    {
        var axiom = input.Axiom.Trim();

        if (axiom.Length == 0)
        {
            return Task.FromResult<Result<IReadOnlyList<string>>>(new Error("axiom.empty", "An axiom needs words."));
        }

        return book.ChangeAsync(
            input.Folder,
            held => held.Contains(axiom)
                ? new Error("axiom.held", "The project already holds that axiom.")
                : Result.Success<IReadOnlyList<string>>([.. held, axiom]),
            cancellationToken);
    }
}
