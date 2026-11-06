using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Page = Overworld.Desktop.Tests.Text.Page;

namespace Overworld.Desktop.Tests.Theming;

public sealed class ChoicesTest
{
    [Theory]
    [InlineData("CheckBoxCheckBackgroundFillChecked")]
    [InlineData("RadioButtonOuterEllipseCheckedFill")]
    public void ChecksInTheThemesAccent(string key)
    {
        TestApp.Run(() =>
        {
            var window = new Window { Content = new CheckBox { IsChecked = true } };
            window.Show();
            Page.Settle();

            Assert.True(window.TryFindResource(key, window.ActualThemeVariant, out var brush));
            Assert.True(window.TryFindResource("AccentColor", window.ActualThemeVariant, out var accent));
            Assert.Equal((Color)accent!, ((SolidColorBrush)brush!).Color);
        });
    }
}
