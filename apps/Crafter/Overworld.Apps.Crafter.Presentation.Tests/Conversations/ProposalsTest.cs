using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Overworld.Allay.Briefs;
using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Axioms;
using Overworld.Apps.Crafter.Presentation.Conversations;
using Overworld.Apps.Crafter.Presentation.Projects;
using Overworld.Apps.Crafter.Projects;

namespace Overworld.Apps.Crafter.Presentation.Tests.Conversations;

public sealed class ProposalsTest : IDisposable
{
    private readonly Bench _bench = new();

    public void Dispose() => _bench.Dispose();

    private async Task<(ProjectViewModel Project, ConversationViewModel Conversation, ToolServer Tools)> TalkingAsync()
    {
        await _bench.HireAsync();
        var folder = await _bench.ProjectAsync("alpha");
        var project = new ProjectViewModel(new Project(folder, "alpha"), _bench.Abilities);
        await project.LoadAsync(Bench.Cancellation);
        await project.NewChatAsync(Bench.Cancellation);
        var conversation = project.Chat!.Conversation;
        await conversation.LoadDeskAsync(Bench.Cancellation);
        _bench.Session.Script(new Finished());
        conversation.Draft = "oi";
        await conversation.SendCommand.ExecuteAsync(null);

        return (project, conversation, Assert.Single(_bench.Agent.Brief!.Tools));
    }

    private static async Task<string> CallAsync(ToolServer tools, string tool, JsonObject arguments)
    {
        var call = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 1,
            ["method"] = "tools/call",
            ["params"] = new JsonObject { ["name"] = tool, ["arguments"] = arguments },
        };
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, tools.Endpoint) { Content = new StringContent(call.ToJsonString(), Encoding.UTF8, "application/json") };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tools.Token);
        using var response = await client.SendAsync(request, Bench.Cancellation);

        return await response.Content.ReadAsStringAsync(Bench.Cancellation);
    }

    private static async Task<T> ShownAsync<T>(ConversationViewModel conversation)
    {
        for (var tries = 0; tries < 500; tries++)
        {
            if (conversation.Timeline.Entries.OfType<T>().FirstOrDefault() is { } shown)
            {
                return shown;
            }

            await Task.Delay(10, Bench.Cancellation);
        }

        throw new TimeoutException($"No {typeof(T).Name} showed.");
    }

    [Fact]
    public async Task KeepsAnAxiomTheUserApprovesAndShowsItAside()
    {
        var (project, conversation, tools) = await TalkingAsync();

        var calling = CallAsync(tools, "propose-axiom", new JsonObject { ["axiom"] = "Never add comments" });
        var card = await ShownAsync<AxiomProposalEntry>(conversation);
        Assert.Equal("Never add comments", card.Axiom);
        card.ApproveCommand.Execute(null);
        var answer = await calling;

        Assert.Contains("approved", answer, StringComparison.Ordinal);
        Assert.Equal(["Never add comments"], (await _bench.Abilities.ListAxioms.ExecuteAsync(new ListAxioms(project.Project.Folder), Bench.Cancellation)).Value);

        for (var tries = 0; tries < 500 && project.Aside.Axioms.Axioms.Count == 0; tries++)
        {
            await Task.Delay(10, Bench.Cancellation);
        }

        Assert.Equal(["Never add comments"], project.Aside.Axioms.Axioms);
        Assert.NotEmpty(_bench.Session.Briefs);
    }

    [Fact]
    public async Task LeavesASummaryTheUserDeclines()
    {
        var (project, conversation, tools) = await TalkingAsync();
        await _bench.Summaries.WriteAsync(project.Project.Folder, "Old line.\nKept line.", Bench.Cancellation);

        var calling = CallAsync(tools, "propose-summary", new JsonObject { ["summary"] = "New line.\nKept line." });
        var card = await ShownAsync<SummaryProposalEntry>(conversation);
        card.ShowChangesCommand.Execute(null);
        card.DeclineCommand.Execute(null);
        var answer = await calling;

        Assert.Contains("declined", answer, StringComparison.Ordinal);
        Assert.Equal(("A new summary for the project", true, "Declined"), (card.Asking, card.ShowsChanges, card.Verdict));
        Assert.Equal(["- Old line.", "+ New line.", "  Kept line."], card.Lines.Select(line => line.Marked));
        Assert.Equal("Old line.\nKept line.", (await _bench.Summaries.ReadAsync(project.Project.Folder, Bench.Cancellation)).Value.Text);
    }
}
