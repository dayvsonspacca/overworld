using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Library;
using Overworld.Apps.Crafter.Presentation.Projects;
using Overworld.Apps.Crafter.Presentation.Workbench;
using Overworld.Apps.Crafter.Summaries;

namespace Overworld.Apps.Crafter.Presentation.Tests.Projects;

public sealed class OverviewViewModelTest : IDisposable
{
    private readonly Bench _bench = new();

    public void Dispose() => _bench.Dispose();

    private async Task<WorkbenchViewModel> OpenAsync(params string[] names)
    {
        foreach (var name in names)
        {
            await _bench.ProjectAsync(name);
        }

        var workbench = new WorkbenchViewModel(_bench.Abilities);
        await workbench.LoadAsync(Bench.Cancellation);
        workbench.Go(0);
        await workbench.Opening;
        await workbench.OpenOverviewCommand.ExecuteAsync(null);

        return workbench;
    }

    [Fact]
    public async Task OpensInTheCenterAndClosesWhenAChatIsPicked()
    {
        var folder = await _bench.ProjectAsync("alpha");
        await _bench.ChatsAsync(folder, Bench.Chat("Bar"));
        var workbench = new WorkbenchViewModel(_bench.Abilities);
        await workbench.LoadAsync(Bench.Cancellation);
        workbench.Go(0);
        await workbench.Opening;
        workbench.Project!.Chat = null;

        await workbench.OpenOverviewCommand.ExecuteAsync(null);

        Assert.True(workbench.Project.ShowsOverview);
        Assert.Equal(("alpha", "Project"), (workbench.Title, workbench.Subtitle));

        workbench.NextChat();

        Assert.False(workbench.Project.ShowsOverview);
        Assert.Equal("Bar", workbench.Title);
    }

    [Fact]
    public async Task ClosesWhenTheChatAlreadyOpenIsPickedAgain()
    {
        await _bench.HireAsync();
        var workbench = await OpenAsync("alpha");
        await workbench.NewChatCommand.ExecuteAsync(null);
        var fresh = workbench.Project!.Chat!;
        await workbench.OpenOverviewCommand.ExecuteAsync(null);

        workbench.Project.PickCommand.Execute(fresh);

        Assert.False(workbench.Project.ShowsOverview);
        Assert.Same(fresh, workbench.Project.Chat);

        await workbench.OpenOverviewCommand.ExecuteAsync(null);
        await workbench.NewChatCommand.ExecuteAsync(null);

        Assert.False(workbench.Project.ShowsOverview);
        Assert.Same(fresh, workbench.Project.Chat);

        await workbench.OpenOverviewCommand.ExecuteAsync(null);
        workbench.NextChat();

        Assert.False(workbench.Project.ShowsOverview);
    }

    [Fact]
    public async Task RenamesTheProjectEverywhere()
    {
        var workbench = await OpenAsync("alpha");
        var overview = workbench.Project!.Overview;

        await workbench.RenameCommand.ExecuteAsync(null);
        overview.NewName = " Overworld ";
        await overview.SaveNameCommand.ExecuteAsync(null);

        Assert.False(overview.IsRenaming);
        Assert.Equal(("Overworld", "O", "Overworld"), (workbench.Project.Name, workbench.Project.Letter, workbench.Title));
        Assert.Equal("Overworld", Assert.Single((await _bench.Abilities.ListProjects.ExecuteAsync(new Apps.Crafter.Projects.ListProjects(), Bench.Cancellation)).Value).Name);
    }

    [Fact]
    public async Task WritesTheSummaryAndRebriefsTheProjectsChats()
    {
        await _bench.HireAsync();
        var workbench = await OpenAsync("alpha");
        var project = workbench.Project!;
        await project.NewChatAsync(Bench.Cancellation);
        await project.Chat!.Conversation.LoadDeskAsync(Bench.Cancellation);
        _bench.Session.Script(new Finished());
        project.Chat.Conversation.Draft = "oi";
        await project.Chat.Conversation.SendCommand.ExecuteAsync(null);
        var overview = project.Overview;

        overview.EditCommand.Execute(null);
        overview.Draft = "# Alpha\n\nA project.";
        await overview.SaveSummaryCommand.ExecuteAsync(null);

        Assert.False(overview.IsEditing);
        Assert.True(overview.HasSummary);
        Assert.Equal("# Alpha\n\nA project.", (await _bench.Abilities.ShowSummary.ExecuteAsync(new ShowSummary(project.Project.Folder), Bench.Cancellation)).Value.Text);
        Assert.Equal("# Alpha\n\nA project.", _bench.Session.Briefs[^1].Context);
    }

    [Fact]
    public async Task GivesASkillAndKeepsTheDefaultOnesOn()
    {
        _bench.Skill("commit", "Writes commits.");
        _bench.Skill("review", "Reviews code.");
        await _bench.Abilities.SetDefaultSkill.ExecuteAsync(new SetDefaultSkill("commit"), Bench.Cancellation);
        var workbench = await OpenAsync("alpha");
        var overview = workbench.Project!.Overview;
        var commit = overview.Skills.Single(skill => skill.Skill.Id == "commit");
        var review = overview.Skills.Single(skill => skill.Skill.Id == "review");

        review.Gets = true;
        for (var tries = 0; tries < 200 && workbench.Project.Aside.Skills.Skills.Count < 2; tries++)
        {
            await Task.Delay(10, Bench.Cancellation);
        }

        Assert.True(commit is { IsDefault: true, Gets: true, CanChange: false });
        Assert.Equal(["commit", "review"], (await _bench.Abilities.ListProjectSkills.ExecuteAsync(new ListProjectSkills(workbench.Project.Project.Folder), Bench.Cancellation)).Value.Select(skill => skill.Id).Order());
        Assert.Equal(2, workbench.Project.Aside.Skills.Skills.Count);
    }

    [Fact]
    public async Task RemovesTheProjectAndGoesHome()
    {
        var workbench = await OpenAsync("alpha", "beta");
        var overview = workbench.Project!.Overview;

        overview.AskToRemoveCommand.Execute(null);
        Assert.True(overview.IsRemoving);
        await overview.RemoveCommand.ExecuteAsync(null);

        Assert.Equal("beta", Assert.Single(workbench.Projects).Name);
        Assert.Null(workbench.Project);
        Assert.True(workbench.ShowsHome);
    }
}
