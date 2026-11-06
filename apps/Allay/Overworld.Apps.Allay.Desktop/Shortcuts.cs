using Avalonia.Controls;
using Avalonia.Media;
using Overworld.Apps.Allay.Presentation;
using Overworld.Desktop;
using Path = Avalonia.Controls.Shapes.Path;

namespace Overworld.Apps.Allay.Desktop;

public static class Shortcuts
{
    private const string Agents = "Agents";

    private const string CheckAll = "Check every agent again";

    public static void Register(Frame frame, AgentsViewModel agents)
    {
        frame.Commands.Add(CheckAll, Agents, () => agents.CheckAllCommand.Execute(null), "Ctrl+R");

        frame.Bar.Trailing = new Button
        {
            Classes = { "icon" },
            Command = agents.CheckAllCommand,
            Content = new Path { Data = Geometry.Parse("M12.5,8 A4.5,4.5 0 1 1 11,4.6 M11.5,2 V5 H8.5") },
            [ToolTip.TipProperty] = $"{CheckAll} · Ctrl+R",
        };
    }
}
