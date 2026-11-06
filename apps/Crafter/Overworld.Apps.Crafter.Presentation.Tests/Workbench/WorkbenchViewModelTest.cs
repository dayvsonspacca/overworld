using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Presentation.Workbench;

namespace Overworld.Apps.Crafter.Presentation.Tests.Workbench;

public sealed class WorkbenchViewModelTest : IDisposable
{
    private readonly Bench _bench = new();

    public void Dispose() => _bench.Dispose();

    [Fact]
    public async Task OpensTheFirstProjectWithItsChats()
    {
        var folder = await _bench.ProjectAsync("alpha");
        await _bench.ProjectAsync("beta");
        await _bench.ChatsAsync(folder, Bench.Chat("Status bar", new Prompt("Add a status bar", [], [])));
        var workbench = new WorkbenchViewModel(_bench.Abilities);

        await workbench.LoadAsync(Bench.Cancellation);

        Assert.Equal(["alpha", "beta"], workbench.Projects.Select(project => project.Name));
        Assert.Same(workbench.Projects[0], workbench.Project);
        Assert.True(workbench.Projects[0].IsCurrent);
        Assert.Equal("Status bar", workbench.Title);
        Assert.Equal("alpha", workbench.Subtitle);
    }

    [Fact]
    public async Task GoesToAProjectBySlotAndIgnoresAnEmptySlot()
    {
        await _bench.ProjectAsync("alpha");
        await _bench.ProjectAsync("beta");
        var workbench = new WorkbenchViewModel(_bench.Abilities);
        await workbench.LoadAsync(Bench.Cancellation);

        workbench.Go(1);
        await workbench.Opening;

        Assert.Equal("beta", workbench.Project?.Name);
        Assert.False(workbench.Projects[0].IsCurrent);
        Assert.True(workbench.Projects[1].IsCurrent);
        Assert.Equal("beta", workbench.Title);
        Assert.Null(workbench.Subtitle);

        workbench.Go(5);

        Assert.Equal("beta", workbench.Project?.Name);
    }

    [Fact]
    public async Task TitlesItselfAfterTheChatPicked()
    {
        var folder = await _bench.ProjectAsync("alpha");
        await _bench.ChatsAsync(folder, Bench.Chat("Older"), Bench.Chat("Newer"));
        var workbench = new WorkbenchViewModel(_bench.Abilities);
        await workbench.LoadAsync(Bench.Cancellation);

        workbench.NextChat();

        Assert.Equal("Older", workbench.Title);
    }

    [Fact]
    public void TogglesTheListAndTheAside()
    {
        var workbench = new WorkbenchViewModel(_bench.Abilities);

        workbench.ToggleList();
        workbench.ToggleAside();
        workbench.ToggleAside();

        Assert.False(workbench.ShowsList);
        Assert.True(workbench.ShowsAside);
    }

    [Fact]
    public void TitlesItselfCrafterWithoutProjects()
    {
        Assert.Equal("Crafter", new WorkbenchViewModel(_bench.Abilities).Title);
    }

    [Fact]
    public async Task MovesAProjectOnTheRailAndRemembersWhereItWent()
    {
        await _bench.ProjectAsync("alpha");
        await _bench.ProjectAsync("beta");
        await _bench.ProjectAsync("gamma");
        var workbench = new WorkbenchViewModel(_bench.Abilities);
        await workbench.LoadAsync(Bench.Cancellation);

        await workbench.MoveProjectAsync(2, 0, Bench.Cancellation);

        Assert.Equal(["gamma", "alpha", "beta"], workbench.Projects.Select(project => project.Name));
        Assert.Equal("alpha", workbench.Project?.Name);

        var again = new WorkbenchViewModel(_bench.Abilities);
        await again.LoadAsync(Bench.Cancellation);

        Assert.Equal(["gamma", "alpha", "beta"], again.Projects.Select(project => project.Name));
    }
}
