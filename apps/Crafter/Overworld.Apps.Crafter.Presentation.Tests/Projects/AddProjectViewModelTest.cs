using System.Diagnostics;
using Overworld.Apps.Crafter.Preferences;
using Overworld.Apps.Crafter.Presentation.Workbench;

namespace Overworld.Apps.Crafter.Presentation.Tests.Projects;

public sealed class AddProjectViewModelTest : IDisposable
{
    private readonly Bench _bench = new();

    public void Dispose() => _bench.Dispose();

    private string Repository(params string[] parts)
    {
        var folder = Directory.CreateDirectory(_bench.PathTo(["code", .. parts])).FullName;
        using var git = Process.Start(new ProcessStartInfo("git", ["init", "-q", folder]) { RedirectStandardOutput = true, RedirectStandardError = true })!;
        git.WaitForExit();

        return folder;
    }

    private async Task<WorkbenchViewModel> ReadyAsync()
    {
        Repository("me", "overworld");
        Repository("me", "zarb");
        Directory.CreateDirectory(_bench.PathTo("code", "notes"));
        await _bench.Abilities.SetProjectsRoot.ExecuteAsync(new SetProjectsRoot(_bench.PathTo("code")), Bench.Cancellation);
        var workbench = new WorkbenchViewModel(_bench.Abilities);
        await workbench.LoadAsync(Bench.Cancellation);

        return workbench;
    }

    [Fact]
    public async Task OffersTheRepositoriesUnderTheProjectsFolderNotYetOpen()
    {
        var already = Repository("abraz", "multi");
        await _bench.AddProject.ExecuteAsync(new Apps.Crafter.Projects.AddProject(already), Bench.Cancellation);
        var workbench = await ReadyAsync();

        await workbench.AddProjectCommand.ExecuteAsync(null);

        Assert.True(workbench.Adding.IsOpen);
        Assert.Equal(["overworld", "zarb"], workbench.Adding.Repositories.Select(choice => choice.Name));
        Assert.Equal("overworld", workbench.Adding.Selected?.Name);

        workbench.Adding.Query = "zar";

        Assert.Equal("zarb", Assert.Single(workbench.Adding.Repositories).Name);
        Assert.Equal("zarb", workbench.Adding.Selected?.Name);
    }

    [Fact]
    public async Task AddsThePickedRepositoryAndOpensIt()
    {
        var workbench = await ReadyAsync();
        await workbench.AddProjectCommand.ExecuteAsync(null);
        workbench.Adding.Query = "zarb";

        await workbench.Adding.AddCommand.ExecuteAsync(null);

        Assert.False(workbench.Adding.IsOpen);
        Assert.Equal("zarb", workbench.Project?.Name);
        Assert.Equal("zarb", Assert.Single(workbench.Projects).Name);
    }

    [Fact]
    public async Task AddsAFolderPickedElsewhere()
    {
        var workbench = await ReadyAsync();
        await workbench.AddProjectCommand.ExecuteAsync(null);

        await workbench.Adding.AddAsync(_bench.PathTo("code", "notes"));

        Assert.Equal("notes", workbench.Project?.Name);
    }

    [Fact]
    public async Task SaysWhyAFolderCannotBeAdded()
    {
        var workbench = await ReadyAsync();
        await workbench.AddProjectCommand.ExecuteAsync(null);

        await workbench.Adding.AddAsync(_bench.PathTo("nowhere"));

        Assert.True(workbench.Adding.IsOpen);
        Assert.NotNull(workbench.Adding.Problem);
    }
}
