using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Overworld.Desktop;

public sealed partial class OverworldApplication(Func<Window> mainWindow) : Application
{
    public static readonly ThemeVariant Amethyst = new("Amethyst", ThemeVariant.Dark);

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.MainWindow = mainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
