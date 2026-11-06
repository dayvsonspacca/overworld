using Overworld.Allay.Hiring;
using Overworld.Bedrock.Results;
using Overworld.Allay.Settings;
using Overworld.Apps.Crafter.Imports;

namespace Overworld.Apps.Crafter.Tests.Imports;

public sealed class ImportOldCrafterTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();

    private readonly OldCrafter _old;

    private readonly StaffFile _staff;

    private readonly ImportOldCrafterOp _import;

    public ImportOldCrafterTest()
    {
        _old = new OldCrafter(_folder.PathTo("config", "crafter"), _folder.PathTo("share", "crafter"));
        _staff = new StaffFile(_folder.PathTo("allay"));
        _import = new ImportOldCrafterOp(_old, _folder.PathTo("overworld", "crafter"), _staff);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _folder.Dispose();

    private void Old()
    {
        Directory.CreateDirectory(_old.Config);
        File.WriteAllText(Path.Combine(_old.Config, "projects.json"), """[{ "folder": "/home/me/overworld", "lastOpened": "2026-11-06T12:00:00-03:00" }]""");
        File.WriteAllText(
            Path.Combine(_old.Config, "preferences.json"),
            """{ "agents": ["claude-code"], "theme": "amethyst", "configurations": { "claude-code": { "tools": "plan", "model": "opus", "effort": "max" } } }""");
        Directory.CreateDirectory(Path.Combine(_old.Data, "chats"));
        File.WriteAllText(Path.Combine(_old.Data, "chats", "overworld-2ed4b88d432d.json"), "[]");
        Directory.CreateDirectory(Path.Combine(_old.Data, "library", "skills", "commit"));
        File.WriteAllText(Path.Combine(_old.Data, "library", "skills", "commit", "instructions.md"), "Commit well.");
    }

    [Fact]
    public async Task CopiesEverythingAndHiresTheAgentsWithTheirSetups()
    {
        Old();

        var done = (await _import.ExecuteAsync(new ImportOldCrafter(), Token)).Value;

        Assert.Equal(
            ["copied the projects", "copied the preferences", "copied the chats of 1 project", "copied the skill library", "hired claude-code in Allay"],
            done);
        Assert.True(File.Exists(_folder.PathTo("overworld", "crafter", "chats", "overworld-2ed4b88d432d.json")));
        Assert.Equal("Commit well.", File.ReadAllText(_folder.PathTo("overworld", "crafter", "library", "skills", "commit", "instructions.md")));
        var staff = (await _staff.ReadAsync(Token)).Value;
        Assert.Equal(["claude-code"], staff.Hired);
        Assert.Equal(new Setup(Mode.Plan, "opus", Effort.Max), staff.Setups!["claude-code"]);
    }

    [Fact]
    public async Task ReplacesNothingAlreadyHere()
    {
        Old();
        Directory.CreateDirectory(_folder.PathTo("overworld", "crafter"));
        File.WriteAllText(_folder.PathTo("overworld", "crafter", "projects.json"), "[]");
        await _staff.UpdateAsync(saved => saved with { Setups = new Dictionary<string, Setup> { ["claude-code"] = new(Mode.Edits) } }, Token);

        var done = (await _import.ExecuteAsync(new ImportOldCrafter(), Token)).Value;

        Assert.Contains("kept the projects already here", done);
        Assert.Equal("[]", File.ReadAllText(_folder.PathTo("overworld", "crafter", "projects.json")));
        Assert.Equal(Mode.Edits, (await _staff.ReadAsync(Token)).Value.Setups!["claude-code"].Mode);
    }

    [Fact]
    public async Task SaysWhenThereIsNoOldCrafter()
    {
        Assert.Equal("import.nothing", (await _import.ExecuteAsync(new ImportOldCrafter(), Token)).Error.Code);
    }
}
