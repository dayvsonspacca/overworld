using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Data;
using Avalonia.Media;
using Overworld.Apps.Crafter.Presentation.Workbench;
using Overworld.Desktop;
using Overworld.Desktop.Windowing;

namespace Overworld.Apps.Crafter.Desktop.Workbench;

public static class Shortcuts
{
    private const string Chats = "Chats";

    private const string Projects = "Projects";

    private const string Window = "Window";

    public static void Register(Frame frame, WorkbenchViewModel workbench, WorkbenchView view)
    {
        var book = frame.Commands;

        book.Add("Next chat", Chats, workbench.NextChat, "Ctrl+Tab");
        book.Add("Previous chat", Chats, workbench.PreviousChat, "Ctrl+Shift+Tab");
        book.Add("Search chats", Chats, view.FocusSearch, "Ctrl+F");

        for (var slot = 0; slot < WorkbenchViewModel.Slots; slot++)
        {
            var at = slot;
            book.Add($"Go to project {slot + 1}", Projects, () => workbench.Go(at), $"Ctrl+{slot + 1}");
        }

        book.Add("Show or hide the chats", Window, workbench.ToggleList, "Ctrl+B");
        book.Add("Show or hide the todos, skills and axioms", Window, workbench.ToggleAside, "Ctrl+Alt+B");

        frame.Bar.DataContext = workbench;
        frame.Bar.Bind(TitleBar.TitleProperty, new Binding(nameof(WorkbenchViewModel.Title)));
        frame.Bar.Bind(TitleBar.SubtitleProperty, new Binding(nameof(WorkbenchViewModel.Subtitle)));
        frame.Bar.Leading = Toggle("Show or hide the chats · Ctrl+B", "M2.5,3.5 H13.5 V12.5 H2.5 Z M6.5,3.5 V12.5", workbench.ToggleListCommand, nameof(WorkbenchViewModel.ShowsList));
        frame.Bar.Trailing = Toggle("Show or hide the aside · Ctrl+Alt+B", "M2.5,3.5 H13.5 V12.5 H2.5 Z M9.5,3.5 V12.5", workbench.ToggleAsideCommand, nameof(WorkbenchViewModel.ShowsAside));
    }

    private static Button Toggle(string tip, string glyph, ICommand command, string shown)
    {
        var button = new Button
        {
            Classes = { "icon" },
            Command = command,
            Content = new Path { Data = Geometry.Parse(glyph) },
            [ToolTip.TipProperty] = tip,
        };
        button.BindClass("chosen", new Binding(shown), button);

        return button;
    }
}
