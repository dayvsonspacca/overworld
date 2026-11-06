using System.Globalization;
using Avalonia.Data.Converters;

namespace Overworld.Desktop.Commands;

public sealed class PaletteOpacity : IValueConverter
{
    public static readonly PaletteOpacity Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is true ? 1.0 : 0.6;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
