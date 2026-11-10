using Overworld.Bedrock.Apps;
using Overworld.Bedrock.Operations;
using Overworld.Hosting;
using Overworld.Seed;
using Overworld.Seed.Steps;

var app = new AppIdentity("seed");
var repository = Repository.Find(Directory.GetCurrentDirectory());

if (!repository.IsSuccess)
{
    await Console.Error.WriteLineAsync($"{repository.Error.Code}: {repository.Error.Message}");

    return 1;
}

var root = repository.Value;
var config = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify);
var share = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);
var bin = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin");
var daemon = Environment.GetEnvironmentVariable("OVERWORLD_BIN") ?? Path.Combine(share, "overworld", "bin");
var apps = Environment.GetEnvironmentVariable("OVERWORLD_APPS") ?? Path.Combine(share, "overworld", "apps");
var fingerprint = new Fingerprint(
[
    root.PathTo("Directory.Build.props"),
    root.PathTo("Directory.Build.rsp"),
    root.PathTo("Directory.Packages.props"),
    root.PathTo("global.json"),
]);

IReadOnlyList<IStep> Plan(Install install) =>
[
    new PackagesStep(root.PathTo("src", "Overworld.Seed", "packages.json")),
    new LinkStep(root.PathTo("config", "hypr"), Path.Combine(config, "hypr")),
    new LinkStep(root.PathTo("config", "quickshell", "overworld"), Path.Combine(config, "quickshell", "overworld")),
    new LinkStep(root.PathTo("config", "kitty", "kitty.conf"), Path.Combine(config, "kitty", "kitty.conf")),
    new LinkStep(root.PathTo("config", "starship", "starship.toml"), Path.Combine(config, "starship.toml")),
    new LinkStep(root.PathTo("config", "zsh", "zshrc"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".zshrc")),
    new LinkStep(root.PathTo("config", "overworld", "themes"), Path.Combine(config, "overworld", "themes")),
    new ThemeStep(root.PathTo("config", "overworld", "themes"), install.Theme, Path.Combine(config, "overworld", "theme.json")),
    new PublishStep(root.PathTo("src", "Overworld.Daemon"), daemon, fingerprint),
    new LinkStep(Path.Combine(daemon, "overworld"), Path.Combine(bin, "overworld")),
    new WindowStep(root.PathTo("src", "overworld-window"), Path.Combine(share, "overworld", "window")),
    new LinkStep(Path.Combine(share, "overworld", "window", WindowStep.Binary), Path.Combine(bin, WindowStep.Binary)),
    new PublishStep(root.PathTo("apps", "Dye", "Overworld.Apps.Dye.Desktop"), Path.Combine(apps, "dye"), fingerprint),
    new DesktopEntryStep("dye", root.PathTo("apps", "Dye"), apps, Path.Combine(share, "applications")),
    new LinkStep(Path.Combine(apps, "dye", "dye"), Path.Combine(bin, "overworld:dye")),
    new PublishStep(root.PathTo("apps", "Crafter", "Overworld.Apps.Crafter.Desktop"), Path.Combine(apps, "crafter"), fingerprint),
    new DesktopEntryStep("crafter", root.PathTo("apps", "Crafter"), apps, Path.Combine(share, "applications")),
    new LinkStep(Path.Combine(apps, "crafter", "crafter"), Path.Combine(bin, "overworld:crafter")),
    new PublishStep(root.PathTo("apps", "Allay", "Overworld.Apps.Allay.Desktop"), Path.Combine(apps, "allay"), fingerprint),
    new DesktopEntryStep("allay", root.PathTo("apps", "Allay"), apps, Path.Combine(share, "applications")),
    new LinkStep(Path.Combine(apps, "allay", "allay"), Path.Combine(bin, "overworld:allay")),
    new PublishStep(root.PathTo("apps", "Painting", "Overworld.Apps.Painting.Desktop"), Path.Combine(apps, "painting"), fingerprint),
    new DesktopEntryStep("painting", root.PathTo("apps", "Painting"), apps, Path.Combine(share, "applications")),
    new LinkStep(Path.Combine(apps, "painting", "painting"), Path.Combine(bin, "overworld:painting")),
];

return await AppHost.RunAsync(app.Command, [Operation.From(new InstallOp(Plan))], args);
