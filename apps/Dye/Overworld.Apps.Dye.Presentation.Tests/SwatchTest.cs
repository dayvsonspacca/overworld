using Overworld.Apps.Dye.Colors;

namespace Overworld.Apps.Dye.Presentation.Tests;

public sealed class SwatchTest
{
    [Fact]
    public void IsLabelledWithTheHexDigits()
    {
        var swatch = Swatch.Of(new Color(0x5B, 0x3A, 0x9C));

        Assert.Equal("5B3A9C", swatch.Label);
        Assert.Equal("#5B3A9C", swatch.Fill);
    }

    [Fact]
    public void WritesWhiteOnADarkColor()
    {
        Assert.Equal("#FFFFFF", Swatch.Of(new Color(0x1C, 0x1B, 0x21)).Ink);
    }

    [Fact]
    public void WritesBlackOnALightColor()
    {
        Assert.Equal("#000000", Swatch.Of(new Color(0xE4, 0xE2, 0xDC)).Ink);
    }
}
