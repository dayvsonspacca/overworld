using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace Overworld.Apps.Crafter.Desktop.Imaging;

public sealed class FileImage : IValueConverter
{
    public static FileImage Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string file && File.Exists(file) ? new Bitmap(file) : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
