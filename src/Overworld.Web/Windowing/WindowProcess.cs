using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace Overworld.Web.Windowing;

public static class WindowProcess
{
    private const string Binary = "overworld-window";

    private const string Override = "OVERWORLD_WINDOW";

    public static Process? Open(string app, string title, Uri url, string? icon)
    {
        var start = new ProcessStartInfo(Locate()) { UseShellExecute = false };
        start.ArgumentList.Add("--app");
        start.ArgumentList.Add(app);
        start.ArgumentList.Add("--title");
        start.ArgumentList.Add(title);
        start.ArgumentList.Add("--url");
        start.ArgumentList.Add(url.ToString());
        start.ArgumentList.Add("--parent");
        start.ArgumentList.Add(Environment.ProcessId.ToString(CultureInfo.InvariantCulture));

        if (icon is not null && File.Exists(icon))
        {
            start.ArgumentList.Add("--icon");
            start.ArgumentList.Add(icon);
        }

        try
        {
            return Process.Start(start);
        }
        catch (Win32Exception)
        {
            return null;
        }
    }

    private static string Locate()
    {
        if (Environment.GetEnvironmentVariable(Override) is { Length: > 0 } chosen)
        {
            return chosen;
        }

        string[] places =
        [
            Path.Combine(AppContext.BaseDirectory, Binary),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "overworld", "window", Binary),
        ];

        return places.FirstOrDefault(File.Exists) ?? Binary;
    }
}
