namespace Overworld.Bedrock.Operations;

public sealed class Catalog
{
    private readonly Dictionary<string, Operation> _byName;

    public Catalog(IEnumerable<Operation> operations)
    {
        All = [.. operations];

        if (All.GroupBy(operation => operation.Name).FirstOrDefault(group => group.Count() > 1) is { } repeated)
        {
            throw new ArgumentException($"Two operations are named '{repeated.Key}'.", nameof(operations));
        }

        _byName = All.ToDictionary(operation => operation.Name);
    }

    public IReadOnlyList<Operation> All { get; }

    public Operation? Find(string name) => _byName.GetValueOrDefault(name);
}
