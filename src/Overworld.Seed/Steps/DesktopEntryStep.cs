using Overworld.Bedrock.Results;

namespace Overworld.Seed.Steps;

public sealed class DesktopEntryStep(string app, string source, string apps, string applications) : IStep
{
    public async Task<Result<string>> RunAsync(CancellationToken cancellationToken)
    {
        var installed = Path.Combine(apps, app);

        if (!Directory.Exists(installed))
        {
            return new Error("seed.missing_source", $"{installed} does not exist; publish {app} before adding it to the launcher.");
        }

        var icon = ((string[])["icon.png", $"{app}.svg"])
            .Select(name => Path.Combine(source, name))
            .FirstOrDefault(File.Exists);

        if (icon is null)
        {
            return new Error("seed.missing_icon", $"{source} has neither icon.png nor {app}.svg.");
        }

        var iconName = "icon" + Path.GetExtension(icon);
        File.Copy(icon, Path.Combine(installed, iconName), overwrite: true);

        var template = await File.ReadAllTextAsync(Path.Combine(source, $"{app}.desktop"), cancellationToken);
        var entry = template
            .Replace("@APPS@", apps, StringComparison.Ordinal)
            .Replace("@ICON@", iconName, StringComparison.Ordinal);

        Directory.CreateDirectory(applications);

        var target = Path.Combine(applications, $"{app}.desktop");
        await File.WriteAllTextAsync(target, entry, cancellationToken);

        return $"added {app} to the app launcher at {target}";
    }
}
