using System.Text.Json;
using Overworld.Apps.Dye.Colors;

namespace Overworld.Apps.Dye.Tests.Colors;

public sealed class ColorTest
{
    [Theory]
    [InlineData("#003F5C")]
    [InlineData("003F5C")]
    [InlineData("#003f5c")]
    public void ParsesSixHexDigitsWithOrWithoutHash(string value)
    {
        var color = Color.Parse(value);

        Assert.True(color.IsSuccess);
        Assert.Equal(new Color(0x00, 0x3F, 0x5C), color.Value);
    }

    [Fact]
    public void ShowsItselfAsUppercaseHexWithHash()
    {
        var color = new Color(0x58, 0x50, 0x8D);

        Assert.Equal("#58508D", color.Hex);
        Assert.Equal("#58508D", color.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("#")]
    [InlineData("#FFF")]
    [InlineData("#003F5C00")]
    [InlineData("#GGGGGG")]
    [InlineData("+12345")]
    [InlineData(" 003F5C")]
    [InlineData("##03F5C")]
    public void RefusesWhatIsNotSixHexDigits(string value)
    {
        var color = Color.Parse(value);

        Assert.False(color.IsSuccess);
        Assert.Equal("color.invalid", color.Error.Code);
        Assert.Contains($"'{value}'", color.Error.Message);
    }

    [Theory]
    [InlineData("#000000", 0)]
    [InlineData("#FFFFFF", 100)]
    [InlineData("#777777", 50)]
    public void KnowsHowLightItLooks(string hex, double lightness)
    {
        Assert.Equal(lightness, Color.Parse(hex).Value.Lightness, precision: 0);
    }

    [Theory]
    [InlineData("#1C1B21", false)]
    [InlineData("#2E2D35", false)]
    [InlineData("#4A4952", false)]
    [InlineData("#5B3A9C", false)]
    [InlineData("#8E62D1", false)]
    [InlineData("#C9A5F2", true)]
    [InlineData("#E4E2DC", true)]
    public void TellsLightColorsFromDarkOnes(string hex, bool light)
    {
        Assert.Equal(light, Color.Parse(hex).Value.IsLight);
    }

    [Fact]
    public void TravelsThroughJsonAsHex()
    {
        var json = JsonSerializer.Serialize(new Color(0x00, 0x3F, 0x5C));

        Assert.Equal("\"#003F5C\"", json);
        Assert.Equal(new Color(0x00, 0x3F, 0x5C), JsonSerializer.Deserialize<Color>(json));
    }

    [Theory]
    [InlineData("\"#zzz\"")]
    [InlineData("42")]
    public void RefusesJsonThatIsNotAHexColor(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Color>(json));
    }
}
