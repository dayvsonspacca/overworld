using Overworld.Apps.Crafter.Library;
using Overworld.Apps.Crafter.Presentation.Workbench;

namespace Overworld.Apps.Crafter.Presentation.Tests.Preferences;

public sealed class PreferencesViewModelTest : IDisposable
{
    private readonly Bench _bench = new();

    public void Dispose() => _bench.Dispose();

    private async Task<WorkbenchViewModel> OpenAsync()
    {
        await _bench.ProjectAsync("alpha");
        var workbench = new WorkbenchViewModel(_bench.Abilities);
        await workbench.LoadAsync(Bench.Cancellation);
        workbench.Go(0);
        await workbench.Opening;
        await workbench.TogglePreferencesCommand.ExecuteAsync(null);

        return workbench;
    }

    [Fact]
    public async Task OpensInPlaceOfTheProjectAndClosesWhenOneIsOpened()
    {
        var workbench = await OpenAsync();

        Assert.True(workbench.ShowsPreferences);
        Assert.Equal(("Preferences", (string?)null), (workbench.Title, workbench.Subtitle));

        workbench.Go(0);

        Assert.False(workbench.ShowsPreferences);
        Assert.Equal("alpha", workbench.Title);
    }

    [Fact]
    public async Task MovesTheProjectsFolder()
    {
        var workbench = await OpenAsync();
        var folder = Directory.CreateDirectory(_bench.PathTo("code")).FullName;

        await workbench.Preferences.SetProjectsRootAsync(folder);

        Assert.Equal(folder, workbench.Preferences.ProjectsRoot);
        Assert.Null(workbench.Preferences.Problem);

        await workbench.Preferences.SetProjectsRootAsync(_bench.PathTo("nowhere"));

        Assert.NotNull(workbench.Preferences.Problem);
        Assert.Equal(folder, workbench.Preferences.ProjectsRoot);
    }

    [Fact]
    public async Task MakesASkillOneEveryProjectGets()
    {
        _bench.Skill("commit", "Writes commits.");
        var workbench = await OpenAsync();
        var preferences = workbench.Preferences;

        Assert.True(preferences.IsLinked);
        preferences.Skills.Single().IsDefault = true;
        for (var tries = 0; tries < 200 && workbench.Project!.Aside.Skills.Skills.Count == 0; tries++)
        {
            await Task.Delay(10, Bench.Cancellation);
        }

        Assert.Equal(["commit"], (await _bench.Abilities.ShowLibrary.ExecuteAsync(new ShowLibrary(), Bench.Cancellation)).Value.Defaults);
        Assert.Equal("commit", Assert.Single(workbench.Project!.Aside.Skills.Skills).Id);
    }

    [Fact]
    public async Task UnlinksTheLibrary()
    {
        _bench.Skill("commit", "Writes commits.");
        var workbench = await OpenAsync();

        await workbench.Preferences.UnlinkCommand.ExecuteAsync(null);

        Assert.False(workbench.Preferences.IsLinked);
        Assert.False(workbench.Preferences.HasSkills);
    }
}
