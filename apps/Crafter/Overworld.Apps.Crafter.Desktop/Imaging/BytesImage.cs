using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace Overworld.Apps.Crafter.Desktop.Imaging;

public sealed class BytesImage : IValueConverter
{
    public static BytesImage Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not byte[] { Length: > 0 } data)
        {
            return null;
        }

        using var stream = new MemoryStream(data);

        try
        {
            return int.TryParse(parameter?.ToString(), CultureInfo.InvariantCulture, out var height)
                ? Bitmap.DecodeToHeight(stream, height)
                : new Bitmap(stream);
        }
        catch (Exception unreadable) when (unreadable is ArgumentException or InvalidOperationException or NotSupportedException or IOException)
        {
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
