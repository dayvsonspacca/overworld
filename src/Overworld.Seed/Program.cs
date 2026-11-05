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
var config = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
var share = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
var bin = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin");
var daemon = Environment.GetEnvironmentVariable("OVERWORLD_BIN") ?? Path.Combine(share, "overworld", "bin");
var apps = Environment.GetEnvironmentVariable("OVERWORLD_APPS") ?? Path.Combine(share, "overworld", "apps");

IStep[] steps =
[
    new LinkStep(root.PathTo("config", "hypr"), Path.Combine(config, "hypr")),
    new LinkStep(root.PathTo("config", "quickshell", "overworld"), Path.Combine(config, "quickshell", "overworld")),
    new PublishStep(root.PathTo("src", "Overworld.Daemon"), daemon),
    new LinkStep(Path.Combine(daemon, "overworld"), Path.Combine(bin, "overworld")),
    new PublishStep(root.PathTo("apps", "Dye", "Overworld.Apps.Dye.Desktop"), Path.Combine(apps, "dye")),
    new DesktopEntryStep("dye", root.PathTo("apps", "Dye"), apps, Path.Combine(share, "applications")),
    new LinkStep(Path.Combine(apps, "dye", "dye"), Path.Combine(bin, "overworld:dye")),
];

return await AppHost.RunAsync(app.Command, [Operation.From(new InstallOp(steps))], args);
