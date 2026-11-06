using Avalonia.Media;
using Overworld.Desktop.Agents;

namespace Overworld.Desktop.Tests.Agents;

public sealed class TintTest
{
    private static readonly Color Orange = Color.Parse("#D97757");

    [Fact]
    public void TurnsTheIconsOwnColorIntoTheOneAsked()
    {
        byte[] pixels = [0x5F, 0xD3, 0xF3, 0xFF];

        Tint.Apply(pixels, Orange, bgra: false, premultiplied: false);

        Assert.Equal([0xD9, 0x77, 0x57, 0xFF], pixels);
    }

    [Fact]
    public void KeepsTheShadingAroundTheColorAsked()
    {
        byte[] pixels = [0x5F, 0xD3, 0xF3, 0xFF, 0x2F, 0x69, 0x79, 0xFF];

        Tint.Apply(pixels, Orange, bgra: false, premultiplied: false);

        Assert.True(pixels[4] < pixels[0] && pixels[5] < pixels[1]);
        Assert.True(pixels[4] > pixels[5] && pixels[5] > pixels[6]);
    }

    [Fact]
    public void ReadsBlueFirstWhenThePixelsAreBgra()
    {
        byte[] pixels = [0xF3, 0xD3, 0x5F, 0xFF];

        Tint.Apply(pixels, Orange, bgra: true, premultiplied: false);

        Assert.Equal([0x57, 0x77, 0xD9, 0xFF], pixels);
    }

    [Fact]
    public void KeepsPremultipliedPixelsPremultiplied()
    {
        byte[] pixels = [0x30, 0x6A, 0x7A, 0x80];

        Tint.Apply(pixels, Orange, bgra: false, premultiplied: true);

        Assert.Equal(0x80, pixels[3]);
        Assert.InRange(pixels[0], 0x6B, 0x6E);
        Assert.InRange(pixels[1], 0x3A, 0x3D);
    }

    [Fact]
    public void LeavesSeeThroughPixelsAndAColorlessIconAlone()
    {
        byte[] clear = [0x5F, 0xD3, 0xF3, 0x00];
        byte[] gray = [0x80, 0x80, 0x80, 0xFF];

        Tint.Apply(clear, Orange, bgra: false, premultiplied: false);
        Tint.Apply(gray, Orange, bgra: false, premultiplied: false);

        Assert.Equal([0x5F, 0xD3, 0xF3, 0x00], clear);
        Assert.Equal([0x80, 0x80, 0x80, 0xFF], gray);
    }
}
