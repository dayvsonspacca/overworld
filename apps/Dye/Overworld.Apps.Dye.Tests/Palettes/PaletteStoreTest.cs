using Overworld.Apps.Dye.Colors;
using Overworld.Apps.Dye.Palettes;

namespace Overworld.Apps.Dye.Tests.Palettes;

public sealed class PaletteStoreTest
{
    private static readonly Palette Sea = new("Sea", [new Color(0x00, 0x3F, 0x5C), new Color(0x58, 0x50, 0x8D)]);

    private static readonly Palette Fire = new("Fire", [new Color(0xFF, 0x63, 0x47)]);

    [Fact]
    public async Task ListsNothingBeforeAnythingIsSaved()
    {
        using var folder = new TemporaryFolder();

        Assert.Empty(await new PaletteStore(folder.Path).ListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task KeepsAddedPalettesInTheOrderTheyCame()
    {
        using var folder = new TemporaryFolder();
        var store = new PaletteStore(folder.Path);

        await store.AddAsync(Sea, TestContext.Current.CancellationToken);
        await store.AddAsync(Fire, TestContext.Current.CancellationToken);

        var palettes = await new PaletteStore(folder.Path).ListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["Sea", "Fire"], palettes.Select(palette => palette.Name));
        Assert.Equal(Sea.Colors, palettes[0].Colors);
        Assert.Equal(Fire.Colors, palettes[1].Colors);
    }

    [Fact]
    public async Task CreatesItsFolderOnTheFirstSave()
    {
        using var folder = new TemporaryFolder();
        var nested = Path.Combine(folder.Path, "overworld", "dye");

        await new PaletteStore(nested).AddAsync(Sea, TestContext.Current.CancellationToken);

        Assert.Equal(["palettes.json"], Directory.GetFiles(nested).Select(Path.GetFileName));
    }

    [Fact]
    public async Task WritesColorsAsHexInTheFile()
    {
        using var folder = new TemporaryFolder();

        await new PaletteStore(folder.Path).AddAsync(Fire, TestContext.Current.CancellationToken);

        var json = await File.ReadAllTextAsync(Path.Combine(folder.Path, "palettes.json"), TestContext.Current.CancellationToken);

        Assert.Contains("\"name\": \"Fire\"", json);
        Assert.Contains("\"#FF6347\"", json);
    }
}
