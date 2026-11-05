using Overworld.Apps.Dye.Colors;
using Overworld.Apps.Dye.Palettes;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Tests.Palettes;

public sealed class ShowPaletteOpTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();

    private readonly PaletteStore _store;

    public ShowPaletteOpTest() => _store = new PaletteStore(_folder.Path);

    public void Dispose() => _folder.Dispose();

    [Fact]
    public async Task ShowsThePaletteWithThatName()
    {
        await Save("Sea", "#003F5C", "#58508D");
        await Save("Fire", "#FF6347");

        var shown = await Show("Sea");

        Assert.True(shown.IsSuccess);
        Assert.Equal("Sea", shown.Value.Name);
        Assert.Equal([new Color(0x00, 0x3F, 0x5C), new Color(0x58, 0x50, 0x8D)], shown.Value.Colors);
    }

    [Fact]
    public async Task TrimsTheName()
    {
        await Save("Sea", "#003F5C");

        Assert.Equal("Sea", (await Show("  Sea ")).Value.Name);
    }

    [Fact]
    public async Task TellsNamesApartByCase()
    {
        await Save("Sea", "#003F5C");
        await Save("sea", "#FFFFFF");

        Assert.Equal([new Color(0xFF, 0xFF, 0xFF)], (await Show("sea")).Value.Colors);
    }

    [Theory]
    [InlineData("Lava")]
    [InlineData("SEA")]
    [InlineData("")]
    public async Task RefusesANameNoPaletteHas(string name)
    {
        await Save("Sea", "#003F5C");

        var shown = await Show(name);

        Assert.False(shown.IsSuccess);
        Assert.Equal("palette.not_found", shown.Error.Code);
        Assert.Equal($"There is no palette named '{name}'.", shown.Error.Message);
    }

    private Task<Result<Palette>> Save(string name, params string[] colors) =>
        new SavePaletteOp(_store).ExecuteAsync(new SavePalette(name, colors), TestContext.Current.CancellationToken);

    private Task<Result<Palette>> Show(string name) =>
        new ShowPaletteOp(_store).ExecuteAsync(new ShowPalette(name), TestContext.Current.CancellationToken);
}
