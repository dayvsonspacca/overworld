using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Projects;

public static class Icons
{
    private const string Home = ".crafter";

    private const string Name = "icon";

    public static Result<string> Adopt(string folder, string image)
    {
        var source = Path.GetFullPath(image);

        if (!File.Exists(source))
        {
            return new Error("project.icon_missing", $"There is no image at '{image}'.");
        }

        var kept = Path.Combine(folder, Home, Name + Path.GetExtension(source).ToLowerInvariant());

        try
        {
            if (source != kept)
            {
                Directory.CreateDirectory(Path.Combine(folder, Home));
                Drop(folder);
                File.Copy(source, kept);
            }
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        {
            return new Error("project.icon_unwritable", $"Could not keep the icon in {Path.Combine(folder, Home)}: {failure.Message}");
        }

        return $"{Home}/{Path.GetFileName(kept)}";
    }

    public static void Drop(string folder)
    {
        var home = Path.Combine(folder, Home);

        if (!Directory.Exists(home))
        {
            return;
        }

        foreach (var icon in Directory.EnumerateFiles(home, Name + ".*"))
        {
            File.Delete(icon);
        }
    }
}
