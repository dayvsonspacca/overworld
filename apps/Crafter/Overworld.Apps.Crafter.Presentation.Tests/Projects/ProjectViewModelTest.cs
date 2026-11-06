using Overworld.Apps.Crafter.Presentation.Projects;
using Overworld.Apps.Crafter.Projects;

namespace Overworld.Apps.Crafter.Presentation.Tests.Projects;

public sealed class ProjectViewModelTest : IDisposable
{
    private readonly Bench _bench = new();

    public void Dispose() => _bench.Dispose();

    private async Task<ProjectViewModel> LoadedAsync(params string[] titles)
    {
        var folder = await _bench.ProjectAsync("alpha");
        await _bench.ChatsAsync(folder, [.. titles.Select(title => Bench.Chat(title))]);
        var project = new ProjectViewModel(new Project(folder, "alpha"), _bench.Abilities);
        await project.LoadAsync(Bench.Cancellation);

        return project;
    }

    [Fact]
    public async Task ListsTheNewestChatFirstAndPicksIt()
    {
        var project = await LoadedAsync("First", "Second", "Third");

        Assert.Equal(["Third", "Second", "First"], project.Chats.Select(chat => chat.Title));
        Assert.Equal("Third", project.Chat?.Title);
    }

    [Fact]
    public async Task FindsChatsByEveryWordTyped()
    {
        var project = await LoadedAsync("Shell status bar", "Seed on a new Arch", "Status of the seed");

        project.Query = "STATUS seed";

        Assert.Equal(["Status of the seed"], project.Chats.Select(chat => chat.Title));
        Assert.False(project.FindsNothing);

        project.Query = "nothing like it";

        Assert.Empty(project.Chats);
        Assert.True(project.FindsNothing);

        project.Query = string.Empty;

        Assert.Equal(3, project.Chats.Count);
    }

    [Fact]
    public async Task StepsThroughTheChatsGoingRound()
    {
        var project = await LoadedAsync("First", "Second", "Third");

        project.Step(1);
        Assert.Equal("Second", project.Chat?.Title);

        project.Step(-2);
        Assert.Equal("First", project.Chat?.Title);
    }

    [Fact]
    public async Task KeepsTheChatPickedWhenLoadingAgain()
    {
        var project = await LoadedAsync("First", "Second");
        project.Step(1);

        await project.LoadAsync(Bench.Cancellation);

        Assert.Equal("First", project.Chat?.Title);
    }

    [Fact]
    public async Task ShowsItsInitialWhenItHasNoIcon()
    {
        var project = await LoadedAsync();

        Assert.Equal("A", project.Letter);
        Assert.False(project.HasIcon);
        Assert.False(project.HasChats);
    }

    [Fact]
    public async Task OpensANewChatAtTheTopAndReusesItWhileEmpty()
    {
        await _bench.HireAsync();
        var project = await LoadedAsync("First");
        project.Query = "first";

        await project.NewChatAsync(Bench.Cancellation);
        var fresh = project.Chat;
        await project.NewChatAsync(Bench.Cancellation);

        Assert.Same(fresh, project.Chat);
        Assert.Equal(["New chat", "First"], project.Chats.Select(chat => chat.Title));
        Assert.Equal(string.Empty, project.Query);
    }

    [Fact]
    public async Task KeepsAnUnsavedChatAndTheLiveOnesWhenLoadingAgain()
    {
        await _bench.HireAsync();
        var project = await LoadedAsync("First");
        var first = project.Chats[0];
        await project.NewChatAsync(Bench.Cancellation);

        await project.LoadAsync(Bench.Cancellation);

        Assert.Equal(2, project.Chats.Count);
        Assert.Same(first, project.Chats[1]);
    }

    [Fact]
    public async Task SaysWhyNoChatCanStartWithoutAnAgent()
    {
        var project = await LoadedAsync();

        await project.NewChatAsync(Bench.Cancellation);

        Assert.Null(project.Chat);
        Assert.Contains("agents-hire", project.Problem, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MarksAChatUnreadWhenItAnswersWhileAnotherIsOpen()
    {
        await _bench.HireAsync();
        var project = await LoadedAsync("Other");
        project.IsCurrent = true;
        await project.NewChatAsync(Bench.Cancellation);
        var talking = project.Chat!;
        await talking.Conversation.LoadDeskAsync(Bench.Cancellation);
        _bench.Session.Script(new Allay.Conversations.Speech("Done"), new Allay.Conversations.Finished());
        _bench.Session.Held = new TaskCompletionSource();
        talking.Conversation.Draft = "go";
        var sending = talking.Conversation.SendCommand.ExecuteAsync(null);
        Assert.True(talking.IsWorking);

        project.Step(1);
        _bench.Session.Held!.SetResult();
        await sending;

        Assert.True(talking.IsUnread);
        Assert.Equal("Done", talking.LastLine);
        Assert.Equal("go", talking.Title);

        project.Step(1);

        Assert.False(talking.IsUnread);
    }
}
