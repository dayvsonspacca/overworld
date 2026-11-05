using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Bedrock.Tests.Operations;

public sealed class CatalogTest
{
    public sealed record Nothing;

    [Operation("mine", "Mines a block.")]
    private sealed class MineOp : IOperation<Nothing, string>
    {
        public Task<Result<string>> ExecuteAsync(Nothing input, CancellationToken cancellationToken) =>
            Task.FromResult<Result<string>>("stone");
    }

    [Operation("craft", "Crafts an item.")]
    private sealed class CraftOp : IOperation<Nothing, string>
    {
        public Task<Result<string>> ExecuteAsync(Nothing input, CancellationToken cancellationToken) =>
            Task.FromResult<Result<string>>("pickaxe");
    }

    [Fact]
    public void FindsAnOperationByItsName()
    {
        var craft = Operation.From(new CraftOp());
        var catalog = new Catalog([Operation.From(new MineOp()), craft]);

        Assert.Same(craft, catalog.Find("craft"));
    }

    [Fact]
    public void FindsNothingForANameNoOperationHas()
    {
        var catalog = new Catalog([Operation.From(new MineOp())]);

        Assert.Null(catalog.Find("smelt"));
    }

    [Fact]
    public void KeepsTheOrderTheOperationsCameIn()
    {
        var catalog = new Catalog([Operation.From(new MineOp()), Operation.From(new CraftOp())]);

        Assert.Equal(["mine", "craft"], catalog.All.Select(operation => operation.Name));
    }

    [Fact]
    public void RefusesTwoOperationsWithTheSameName()
    {
        var thrown = Assert.Throws<ArgumentException>(() =>
            new Catalog([Operation.From(new MineOp()), Operation.From(new MineOp())]));

        Assert.Contains("'mine'", thrown.Message);
    }
}
