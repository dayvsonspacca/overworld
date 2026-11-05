using Overworld.Apps.Dye.Colors;
using Overworld.Apps.Dye.Palettes;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Tests.Palettes;

public sealed class SavePaletteOpTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();

    private readonly PaletteStore _store;

    private readonly SavePaletteOp _save;

    public SavePaletteOpTest()
    {
        _store = new PaletteStore(_folder.Path);
        _save = new SavePaletteOp(_store);
    }

    public void Dispose() => _folder.Dispose();

    [Fact]
    public async Task SavesAPaletteWithItsColorsInOrder()
    {
        var saved = await Save("Sea", "#003f5c", "58508D");

        Assert.True(saved.IsSuccess);
        Assert.Equal("Sea", saved.Value.Name);
        Assert.Equal([new Color(0x00, 0x3F, 0x5C), new Color(0x58, 0x50, 0x8D)], saved.Value.Colors);

        var stored = Assert.Single(await Stored());
        Assert.Equal("Sea", stored.Name);
        Assert.Equal(saved.Value.Colors, stored.Colors);
    }

    [Fact]
    public async Task TrimsTheName()
    {
        var saved = await Save("  Sea ", "#003F5C");

        Assert.Equal("Sea", saved.Value.Name);
    }

    [Fact]
    public async Task RefusesANameThatIsTaken()
    {
        await Save("Sea", "#003F5C");

        var again = await Save("Sea", "#FFFFFF");

        AssertFails("palette.exists", again);
        Assert.Equal([new Color(0x00, 0x3F, 0x5C)], Assert.Single(await Stored()).Colors);
    }

    [Fact]
    public async Task TellsNamesApartByCase()
    {
        await Save("Sea", "#003F5C");

        var lower = await Save("sea", "#FFFFFF");

        Assert.True(lower.IsSuccess);
        Assert.Equal(["Sea", "sea"], (await Stored()).Select(palette => palette.Name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RefusesAPaletteWithoutAName(string name)
    {
        AssertFails("palette.name_empty", await Save(name, "#003F5C"));
        Assert.Empty(await Stored());
    }

    [Fact]
    public async Task RefusesAPaletteWithoutColors()
    {
        AssertFails("palette.colors_empty", await Save("Sea"));
        Assert.Empty(await Stored());
    }

    [Fact]
    public async Task RefusesAnInvalidColorAndSavesNothing()
    {
        var saved = await Save("Sea", "#003F5C", "#zzz");

        AssertFails("color.invalid", saved);
        Assert.Contains("'#zzz'", saved.Error.Message);
        Assert.Empty(await Stored());
    }

    private Task<Result<Palette>> Save(string name, params string[] colors) =>
        _save.ExecuteAsync(new SavePalette(name, colors), TestContext.Current.CancellationToken);

    private async Task<IReadOnlyList<Palette>> Stored() => (await _store.ListAsync(TestContext.Current.CancellationToken)).Value;

    private static void AssertFails(string code, Result<Palette> result)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(code, result.Error.Code);
    }

    [Fact]
    public async Task PassesOnAFileItCannotRead()
    {
        await File.WriteAllTextAsync(Path.Combine(_folder.Path, "palettes.json"), "{ not json", TestContext.Current.CancellationToken);

        AssertFails("file.unreadable", await Save("Sea", "#003F5C"));
    }
}
