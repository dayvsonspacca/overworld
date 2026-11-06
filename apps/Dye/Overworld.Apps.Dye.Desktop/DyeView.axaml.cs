using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Overworld.Apps.Dye.Presentation;
using Overworld.Desktop;

namespace Overworld.Apps.Dye.Desktop;

public sealed partial class DyeView : UserControl
{
    private const string Window = "Window";

    public DyeView() => InitializeComponent();

    public static void Dress(Frame frame, DyeViewModel dye)
    {
        frame.Commands.Add("Palettes", Window, () => dye.ShowPalettesCommand.Execute(null), "Ctrl+1");
        frame.Commands.Add("Themes", Window, () => dye.ShowThemesCommand.Execute(null), "Ctrl+2");

        frame.Bar.DataContext = dye;
        frame.Bar.Trailing = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                Chip("Palettes", "Palettes · Ctrl+1", dye.ShowPalettesCommand, nameof(DyeViewModel.ShowsPalettes)),
                Chip("Themes", "Themes · Ctrl+2", dye.ShowThemesCommand, nameof(DyeViewModel.ShowsThemes)),
            },
        };
    }

    private static Button Chip(string label, string tip, System.Windows.Input.ICommand command, string chosen)
    {
        var chip = new Button
        {
            Classes = { "chip" },
            Content = label,
            Command = command,
            [ToolTip.TipProperty] = tip,
        };
        chip.BindClass("chosen", new Binding(chosen), chip);

        return chip;
    }
}
