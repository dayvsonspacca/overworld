using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Overworld.Allay.Briefs;
using Overworld.Apps.Crafter.Axioms;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Library;
using Overworld.Apps.Crafter.Preferences;
using Overworld.Apps.Crafter.Projects;
using Overworld.Apps.Crafter.Sessions;
using Overworld.Apps.Crafter.Summaries;
using Overworld.Apps.Crafter.Todos;
using Overworld.Apps.Crafter.Tests.Library;
using Overworld.Bedrock.Operations;
using Overworld.Mcp;

namespace Overworld.Apps.Crafter.Tests.Sessions;

public sealed class SessionsTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();
    private readonly Shelf _shelf;
    private readonly string _overworld;

    public SessionsTest()
    {
        _shelf = new Shelf(_folder.PathTo("data"));
        _overworld = Directory.CreateDirectory(_folder.PathTo("overworld")).FullName;
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _folder.Dispose();

    [Fact]
    public async Task BriefsAProjectWithItsSummaryAxiomsAndSkills()
    {
        var remote = new Remote(_folder.PathTo("remote"));
        remote.Skill("commit", """{ "name": "Commit", "description": "Writes commits.", "whenToUse": "Committing" }""").Publish();
        var preferences = new PreferencesStore(_folder.PathTo("data"), "/home/someone");
        var library = new SkillLibrary(preferences, new Mirror(_folder.PathTo("data", "library")));
        await library.LinkAsync(remote.Repository, Token);
        await library.MakeDefaultAsync("commit", true, Token);
        await _shelf.AddAsync(_overworld, Token);
        var settings = new SettingsStore();
        var axioms = new AxiomBook(_shelf, settings);
        await axioms.ChangeAsync(_overworld, _ => Bedrock.Results.Result.Success<IReadOnlyList<string>>(["Never add comments."]), Token);
        var summaries = new SummaryStore();
        await summaries.WriteAsync(_overworld, "Overworld is a desktop.", Token);
        ToolServer[] tools = [new("crafter", new Uri("http://127.0.0.1:1/mcp"), "t", [])];

        var brief = await new ProjectBrief(summaries, axioms, new ProjectSkills(_shelf, library)).OfAsync(_overworld, tools, Token);

        Assert.Equal(_overworld, brief.Value.Folder);
        Assert.Equal("Overworld is a desktop.", brief.Value.Context);
        Assert.Equal(["Never add comments."], brief.Value.Rules);
        var skill = Assert.Single(brief.Value.Skills);
        Assert.Equal("Writes commits. Use it when: Committing", skill.Description);
        Assert.Equal(Path.Combine(_folder.PathTo("data", "library"), "skills", "commit", "instructions.md"), skill.InstructionsFile);
        Assert.Equal(tools, brief.Value.Tools);
    }

    [Fact]
    public async Task RefusesToBriefAProjectThatIsNotOpen()
    {
        var preferences = new PreferencesStore(_folder.PathTo("data"), "/home/someone");
        var brief = await new ProjectBrief(new SummaryStore(), new AxiomBook(_shelf, new SettingsStore()), new ProjectSkills(_shelf, new SkillLibrary(preferences, new Mirror(_folder.PathTo("lib")))))
            .OfAsync(_overworld, [], Token);

        Assert.Equal("project.not_open", brief.Error.Code);
    }

    [Fact]
    public async Task GivesASessionTheTodosOfItsOwnProjectOnly()
    {
        await _shelf.AddAsync(_overworld, Token);
        using var host = new McpHttpHost("crafter");
        host.Start();
        var todos = new TodoList(_shelf);
        var server = new Toolbox(host, [Operation.From(new ListTodosOp(todos))], [Operation.From(new AddTodoOp(todos)), Operation.From(new CompleteTodoOp(todos)), Operation.From(new RemoveTodoOp(todos))], new AxiomBook(_shelf, new SettingsStore()), new SummaryStore()).Admit(_overworld);

        var listed = await PostAsync(server, """{"jsonrpc":"2.0","id":1,"method":"tools/list"}""");
        await PostAsync(server, """{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"add-todo","arguments":{"folder":"/elsewhere","text":"Modal"}}}""");

        Assert.Equal(["list-todos", "add-todo", "complete-todo", "remove-todo"], server.FreeTools);
        Assert.Equal(Toolbox.Patience, server.Patience);
        Assert.Equal(4, listed["result"]!["tools"]!.AsArray().Count);
        Assert.Equal([new Todo(1, "Modal")], (await todos.ListAsync(_overworld, Token)).Value);
    }

    [Fact]
    public async Task TellsWhatAToolChangedAndLetsAChatBeRenamed()
    {
        await _shelf.AddAsync(_overworld, Token);
        using var host = new McpHttpHost("crafter");
        host.Start();
        var todos = new TodoList(_shelf);
        var kept = 0;
        ChatTitle? title = null;
        var toolbox = new Toolbox(host, [Operation.From(new ListTodosOp(todos))], [Operation.From(new AddTodoOp(todos))], new AxiomBook(_shelf, new SettingsStore()), new SummaryStore());
        var plain = toolbox.Admit(_overworld);
        var server = toolbox.Admit(_overworld, kept: () => kept++, retitle: given => title = given);

        await PostAsync(server, """{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"list-todos","arguments":{}}}""");
        await PostAsync(server, """{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"add-todo","arguments":{"text":"Pictures"}}}""");
        await PostAsync(server, """{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"rename-chat","arguments":{"title":"Pictures"}}}""");

        Assert.DoesNotContain("rename-chat", plain.FreeTools);
        Assert.Contains("rename-chat", server.FreeTools);
        Assert.Equal(1, kept);
        Assert.Equal(ChatTitle.Of("Pictures"), title);
    }

    [Fact]
    public async Task ForgetsASessionItReleased()
    {
        await _shelf.AddAsync(_overworld, Token);
        using var host = new McpHttpHost("crafter");
        host.Start();
        var toolbox = new Toolbox(host, [], [], new AxiomBook(_shelf, new SettingsStore()), new SummaryStore());
        var server = toolbox.Admit(_overworld);

        toolbox.Release(server);

        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, server.Endpoint) { Content = new StringContent("""{"jsonrpc":"2.0","id":1,"method":"ping"}""", Encoding.UTF8, "application/json") };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", server.Token);
        using var response = await client.SendAsync(request, Token);
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void FindsTheSharedInstructionsBeforeAnAgentsOwn()
    {
        File.WriteAllText(Path.Combine(_overworld, "CLAUDE.md"), "Claude's own.");
        Assert.Equal(new Instructions("CLAUDE.md", "Claude's own."), Instructions.In(_overworld, ["CLAUDE.md"]));

        File.WriteAllText(Path.Combine(_overworld, "AGENTS.md"), " For any agent. ");
        Assert.Equal(new Instructions("AGENTS.md", "For any agent."), Instructions.In(_overworld, ["CLAUDE.md"]));
        Assert.Null(Instructions.In(_folder.PathTo("data"), ["CLAUDE.md"]));
    }

    private static async Task<JsonNode> PostAsync(ToolServer server, string json)
    {
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, server.Endpoint) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", server.Token);
        using var response = await client.SendAsync(request, Token);

        return JsonNode.Parse(await response.Content.ReadAsStringAsync(Token))!;
    }
}
