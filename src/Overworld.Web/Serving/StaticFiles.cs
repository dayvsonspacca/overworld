namespace Overworld.Web.Serving;

public sealed class StaticFiles(string root)
{
    private static readonly Dictionary<string, string> Kinds = new(StringComparer.OrdinalIgnoreCase)
    {
        [".html"] = "text/html; charset=utf-8",
        [".js"] = "text/javascript; charset=utf-8",
        [".mjs"] = "text/javascript; charset=utf-8",
        [".css"] = "text/css; charset=utf-8",
        [".json"] = "application/json",
        [".png"] = "image/png",
        [".svg"] = "image/svg+xml",
        [".woff2"] = "font/woff2",
    };

    public string? Find(string path)
    {
        var relative = Uri.UnescapeDataString(path).TrimStart('/');

        if (relative.Length == 0)
        {
            relative = "app/index.html";
        }
        else if (!relative.StartsWith("kit/", StringComparison.Ordinal))
        {
            relative = $"app/{relative}";
        }

        var full = Path.GetFullPath(Path.Combine(root, relative));

        return full.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.Ordinal) && File.Exists(full) ? full : null;
    }

    public static string Kind(string file) => Kinds.GetValueOrDefault(Path.GetExtension(file), "application/octet-stream");
}
