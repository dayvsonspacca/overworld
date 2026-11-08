using System.Text.Json;
using Overworld.Apps.Dye.Presentation.Lending;

namespace Overworld.Apps.Dye.Presentation.Tests.Lending;

public sealed class DyeLenderTest
{
    private readonly DyeLender _lender = new();

    private static JsonElement Said(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void LendsThePaletteShowGaveBack()
    {
        var loan = Assert.IsType<PaletteLoan>(_lender.Lend(Said("""{ "name": "Sea", "colors": ["#003F5C", "#FFA600"] }""")));

        Assert.Equal(("dye:palette:Sea", "Dye", "Sea · 2 colors"), (loan.Key, loan.Source, loan.Title));
        Assert.Equal(["003F5C", "FFA600"], loan.Swatches.Select(swatch => swatch.Label));
    }

    [Fact]
    public void LendsTheThemeInTheVariantWorn()
    {
        var loan = Assert.IsType<ThemeLoan>(_lender.Lend(Said(
            """{ "name": "amethyst", "worn": "light", "dark": { "background": "#1C1B21" }, "light": { "background": "#F6F4F9", "accent": "#7D53BF" } }""")));

        Assert.Equal(("dye:theme:amethyst", "amethyst · light · worn"), (loan.Key, loan.Title));
        Assert.Equal(["Surfaces", "Accent"], loan.Groups.Select(group => group.Title));
        Assert.Equal("#F6F4F9", loan.Groups[0].Swatches[0].Hex);
    }

    [Fact]
    public void LendsAThemeNotWornInItsDarkVariant()
    {
        var loan = Assert.IsType<ThemeLoan>(_lender.Lend(Said(
            """{ "name": "warden", "worn": null, "dark": { "background": "#101418" }, "light": { "background": "#F0F4F8" } }""")));

        Assert.Equal("warden · dark", loan.Title);
        Assert.Equal("#101418", loan.Groups[0].Swatches[0].Hex);
    }

    [Theory]
    [InlineData("""{ "name": "Sea", "colors": [] }""")]
    [InlineData("""{ "name": "Sea", "colors": ["not a color"] }""")]
    [InlineData("""{ "id": "1d9b883c-b335-4dc4-b228-6bf3aeb6802c", "title": "A", "html": "<p></p>", "painted": "2026-11-07T12:00:00+00:00" }""")]
    [InlineData("""{ "name": "amethyst", "dark": { "background": "#1C1B21" } }""")]
    [InlineData("[1, 2]")]
    public void LendsNothingElse(string said)
    {
        Assert.Null(_lender.Lend(Said(said)));
    }
}
