using Overworld.Apps.Dye.Colors;
using Overworld.Apps.Dye.Palettes;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Tests.Palettes;

public sealed class ListPalettesOpTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();

    private readonly PaletteStore _store;

    public ListPalettesOpTest() => _store = new PaletteStore(_folder.Path);

    public void Dispose() => _folder.Dispose();

    [Fact]
    public async Task ListsNothingBeforeAnythingIsSaved()
    {
        var listed = await List();

        Assert.True(listed.IsSuccess);
        Assert.Empty(listed.Value);
    }

    [Fact]
    public async Task ListsEveryPaletteWithItsColorsInTheOrderTheyWereSaved()
    {
        var save = new SavePaletteOp(_store);
        await save.ExecuteAsync(new SavePalette("Sea", ["#003F5C", "#58508D"]), TestContext.Current.CancellationToken);
        await save.ExecuteAsync(new SavePalette("Fire", ["#FF6347"]), TestContext.Current.CancellationToken);

        var listed = (await List()).Value;

        Assert.Equal(["Sea", "Fire"], listed.Select(palette => palette.Name));
        Assert.Equal([new Color(0x00, 0x3F, 0x5C), new Color(0x58, 0x50, 0x8D)], listed[0].Colors);
        Assert.Equal([new Color(0xFF, 0x63, 0x47)], listed[1].Colors);
    }

    private Task<Result<IReadOnlyList<Palette>>> List() =>
        new ListPalettesOp(_store).ExecuteAsync(new ListPalettes(), TestContext.Current.CancellationToken);

    [Fact]
    public async Task PassesOnAFileItCannotRead()
    {
        await File.WriteAllTextAsync(Path.Combine(_folder.Path, "palettes.json"), "{ not json", TestContext.Current.CancellationToken);

        Assert.Equal("file.unreadable", (await List()).Error.Code);
    }
}
