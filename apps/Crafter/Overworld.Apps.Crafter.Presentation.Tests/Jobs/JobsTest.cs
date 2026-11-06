using Overworld.Allay.Conversations;
using Overworld.Allay.Jobs;
using Overworld.Apps.Crafter.Presentation.Aside;
using Overworld.Apps.Crafter.Presentation.Projects;
using Overworld.Apps.Crafter.Projects;

namespace Overworld.Apps.Crafter.Presentation.Tests.Jobs;

public sealed class JobsTest : IDisposable
{
    private readonly Bench _bench = new();

    public void Dispose() => _bench.Dispose();

    private async Task<ProjectViewModel> TalkingAsync()
    {
        await _bench.HireAsync();
        var folder = await _bench.ProjectAsync("alpha");
        var project = new ProjectViewModel(new Project(folder, "alpha"), _bench.Abilities);
        await project.LoadAsync(Bench.Cancellation);
        await project.NewChatAsync(Bench.Cancellation);
        var conversation = project.Chat!.Conversation;
        await conversation.LoadDeskAsync(Bench.Cancellation);
        _bench.Session.Script(new Finished());
        conversation.Draft = "build in the background";
        await conversation.SendCommand.ExecuteAsync(null);

        return project;
    }

    [Fact]
    public async Task ListsTheJobsOfTheOpenChatAndKeepsTheOnesStillRunning()
    {
        var project = await TalkingAsync();
        var conversation = project.Chat!.Conversation;

        _bench.Session.Report(new Job("j-1", JobKind.Command, "dotnet watch test", Prints: true), new Job("j-2", JobKind.Agent, "find the theme files"));
        var first = conversation.Jobs[0];
        _bench.Session.Report(new Job("j-1", JobKind.Command, "dotnet watch test", Prints: true));

        Assert.Same(first, Assert.Single(conversation.Jobs));
        Assert.Same(conversation, project.Aside.Conversation);
        Assert.True(conversation.HasJobs);
    }

    [Fact]
    public async Task OpensAJobsOutputAndStopsIt()
    {
        var project = await TalkingAsync();
        _bench.Session.Report(new Job("j-1", JobKind.Monitor, "tail the log"));
        var job = project.Chat!.Conversation.Jobs[0];

        job.ToggleCommand.Execute(null);
        job.StopCommand.Execute(null);

        Assert.Equal(("Watching", true, "output of j-1"), (job.Kind, job.IsOpen, job.Output));
        Assert.Equal(["j-1"], _bench.Session.Stopped);
    }

    [Fact]
    public async Task LeavesTheJobsTabWhenTheLastJobEnds()
    {
        var project = await TalkingAsync();
        _bench.Session.Report(new Job("j-1", JobKind.Command, "npm run build"));
        project.Aside.ShowJobs();
        Assert.True(project.Aside.ShowsJobs);

        _bench.Session.Report();

        Assert.Equal(AsideTab.Todos, project.Aside.Tab);
    }
}
