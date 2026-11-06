using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Axioms;

[Operation("list-axioms", "Lists a project's axioms, the rules its agent follows in every chat, in the order they were added.")]
public sealed class ListAxiomsOp(AxiomBook book) : IOperation<ListAxioms, IReadOnlyList<string>>
{
    public Task<Result<IReadOnlyList<string>>> ExecuteAsync(ListAxioms input, CancellationToken cancellationToken) =>
        book.ListAsync(input.Folder, cancellationToken);
}
