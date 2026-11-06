using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Overworld.Desktop.Agents;

public sealed class AllayIcon : Control
{
    public static readonly StyledProperty<string?> ColorProperty =
        AvaloniaProperty.Register<AllayIcon, string?>(nameof(Color));

    private static readonly Uri Source = new("avares://Overworld.Desktop/Agents/allay.png");

    private static readonly ConcurrentDictionary<string, WriteableBitmap> Tinted = new(StringComparer.OrdinalIgnoreCase);

    static AllayIcon() => AffectsRender<AllayIcon>(ColorProperty);

    public string? Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var bitmap = Tinted.GetOrAdd(Avalonia.Media.Color.TryParse(Color, out var color) ? color.ToString() : string.Empty, Paint);
        var side = Math.Min(Bounds.Width, Bounds.Height);
        var area = new Rect((Bounds.Width - side) / 2, (Bounds.Height - side) / 2, side, side);

        using (context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.HighQuality }))
        {
            context.DrawImage(bitmap, area);
        }
    }

    private static WriteableBitmap Paint(string color)
    {
        using var stream = AssetLoader.Open(Source);
        var bitmap = WriteableBitmap.Decode(stream);

        if (color.Length == 0)
        {
            return bitmap;
        }

        using var buffer = bitmap.Lock();
        var pixels = new byte[buffer.RowBytes * buffer.Size.Height];
        Marshal.Copy(buffer.Address, pixels, 0, pixels.Length);
        Tint.Apply(pixels, Avalonia.Media.Color.Parse(color), buffer.Format == PixelFormat.Bgra8888, buffer.AlphaFormat == AlphaFormat.Premul);
        Marshal.Copy(pixels, 0, buffer.Address, pixels.Length);

        return bitmap;
    }
}
