using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Presentation.Workbench;

namespace Overworld.Apps.Crafter.Presentation.Tests.Workbench;

public sealed class WorkbenchViewModelTest : IDisposable
{
    private readonly Bench _bench = new();

    public void Dispose() => _bench.Dispose();

    [Fact]
    public async Task OpensHomeWithEveryProjectSurveyed()
    {
        var folder = await _bench.ProjectAsync("alpha");
        await _bench.ProjectAsync("beta");
        await _bench.ChatsAsync(folder, Bench.Chat("Status bar", new Prompt("Add a status bar", [], [])));
        var workbench = new WorkbenchViewModel(_bench.Abilities);

        await workbench.LoadAsync(Bench.Cancellation);

        Assert.Equal(["alpha", "beta"], workbench.Projects.Select(project => project.Name));
        Assert.Null(workbench.Project);
        Assert.True(workbench.ShowsHome);
        Assert.All(workbench.Projects, project => Assert.False(project.IsCurrent));
        Assert.Equal("Crafter", workbench.Title);
        Assert.Null(workbench.Subtitle);
        Assert.Equal([1, 0], workbench.Projects.Select(project => project.ChatCount));
        Assert.Equal("Add a status bar", workbench.Projects[0].LastLine);
        Assert.Null(workbench.Projects[1].LastLine);
    }

    [Fact]
    public async Task GoesHomeFromAProject()
    {
        await _bench.ProjectAsync("alpha");
        var workbench = new WorkbenchViewModel(_bench.Abilities);
        await workbench.LoadAsync(Bench.Cancellation);
        workbench.Go(0);
        await workbench.Opening;

        await workbench.GoHomeCommand.ExecuteAsync(null);

        Assert.Null(workbench.Project);
        Assert.True(workbench.ShowsHome);
        Assert.False(workbench.Projects[0].IsCurrent);
    }

    [Fact]
    public async Task ListsTheChatsLastTouchedInEveryProjectNewestFirst()
    {
        var alpha = await _bench.ProjectAsync("alpha");
        var beta = await _bench.ProjectAsync("beta");
        var morning = new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);
        await _bench.ChatsAsync(alpha, Bench.Chat("Old") with { Touched = morning }, Bench.Chat("Never"));
        await _bench.ChatsAsync(beta, Bench.Chat("New") with { Touched = morning.AddHours(2) });
        var workbench = new WorkbenchViewModel(_bench.Abilities);

        await workbench.LoadAsync(Bench.Cancellation);

        Assert.True(workbench.Home.HasRecent);
        Assert.Equal(["New", "Old"], workbench.Home.Recent.Select(recent => recent.Chat.Title));
        Assert.Equal(["beta", "alpha"], workbench.Home.Recent.Select(recent => recent.Project.Name));
    }

    [Fact]
    public async Task OpensARecentChatInItsProject()
    {
        var alpha = await _bench.ProjectAsync("alpha");
        await _bench.ChatsAsync(alpha, Bench.Chat("Old") with { Touched = DateTimeOffset.UnixEpoch }, Bench.Chat("Newer"));
        var workbench = new WorkbenchViewModel(_bench.Abilities);
        await workbench.LoadAsync(Bench.Cancellation);

        workbench.OpenRecent(workbench.Home.Recent.Single());
        await workbench.Opening;

        Assert.Equal("alpha", workbench.Project?.Name);
        Assert.Equal("Old", workbench.Project?.Chat?.Title);
        Assert.False(workbench.ShowsHome);
    }

    [Fact]
    public async Task StartsANewChatInAProjectFromHome()
    {
        await _bench.HireAsync();
        await _bench.ProjectAsync("alpha");
        await _bench.ProjectAsync("beta");
        var workbench = new WorkbenchViewModel(_bench.Abilities);
        await workbench.LoadAsync(Bench.Cancellation);

        await workbench.NewChatInCommand.ExecuteAsync(workbench.Projects[1]);
        await workbench.Opening;

        Assert.Equal("beta", workbench.Project?.Name);
        Assert.True(workbench.Project?.Chat?.IsFresh);
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
        workbench.Go(0);
        await workbench.Opening;

        workbench.NextChat();

        Assert.Equal("Older", workbench.Title);
    }

    [Fact]
    public void TogglesTheListAndTheAside()
    {
        var workbench = new WorkbenchViewModel(_bench.Abilities);

        workbench.ToggleList();
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
        workbench.Go(0);
        await workbench.Opening;

        await workbench.MoveProjectAsync(2, 0, Bench.Cancellation);

        Assert.Equal(["gamma", "alpha", "beta"], workbench.Projects.Select(project => project.Name));
        Assert.Equal("alpha", workbench.Project?.Name);

        var again = new WorkbenchViewModel(_bench.Abilities);
        await again.LoadAsync(Bench.Cancellation);

        Assert.Equal(["gamma", "alpha", "beta"], again.Projects.Select(project => project.Name));
    }
}
