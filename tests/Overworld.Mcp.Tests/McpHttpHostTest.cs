using System.ComponentModel;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Mcp.Tests;

public sealed class McpHttpHostTest : IDisposable
{
    public sealed record AddTodo(
        [property: Description("The project's folder.")] string Folder,
        [property: Description("What is left to do.")] string Text,
        int Priority = 0);

    [Operation("add-todo", "Adds a todo to a project.")]
    private sealed class AddTodoOp : IOperation<AddTodo, AddTodo>
    {
        public Task<Result<AddTodo>> ExecuteAsync(AddTodo input, CancellationToken cancellationToken) =>
            Task.FromResult<Result<AddTodo>>(input.Text.Length == 0 ? new Error("todo.empty", "A todo needs words.") : input);
    }

    private static readonly Caller Session = new(
        "session-1",
        [new ServedTool(Operation.From(new AddTodoOp()), new Dictionary<string, JsonNode> { ["folder"] = "/work/overworld" })],
        "Keep the project's todos.");

    private readonly McpHttpHost _host = new("crafter");

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _host.Dispose();

    private static string Call(string tool, string arguments) =>
        "{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"tools/call\",\"params\":{\"name\":\"" + tool + "\",\"arguments\":" + arguments + "}}";

    [Fact]
    public async Task IntroducesItselfWithTheCallersInstructions()
    {
        var answer = await _host.AnswerAsync("""{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26"}}""", Session);

        Assert.Equal("2025-03-26", answer!["result"]!["protocolVersion"]!.GetValue<string>());
        Assert.Equal("crafter", answer["result"]!["serverInfo"]!["name"]!.GetValue<string>());
        Assert.Equal("Keep the project's todos.", answer["result"]!["instructions"]!.GetValue<string>());
    }

    [Fact]
    public async Task OffersItsNewestVersionToAClientAskingForAnUnknownOne()
    {
        var answer = await _host.AnswerAsync("""{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"1999-01-01"}}""", Session);

        Assert.Equal("2025-06-18", answer!["result"]!["protocolVersion"]!.GetValue<string>());
    }

    [Fact]
    public async Task ListsTheCallersToolsWithoutWhatTheAppSetsForThem()
    {
        var answer = await _host.AnswerAsync("""{"jsonrpc":"2.0","id":2,"method":"tools/list"}""", Session);

        var tool = answer!["result"]!["tools"]![0]!;
        Assert.Equal("add-todo", tool["name"]!.GetValue<string>());
        Assert.Equal("Adds a todo to a project.", tool["description"]!.GetValue<string>());
        var properties = tool["inputSchema"]!["properties"]!.AsObject();
        Assert.False(properties.ContainsKey("folder"));
        Assert.Equal("What is left to do.", properties["text"]!["description"]!.GetValue<string>());
        Assert.DoesNotContain("folder", tool["inputSchema"]!["required"]!.AsArray().Select(name => name!.GetValue<string>()));
    }

    [Fact]
    public async Task CallsAToolWithWhatTheAppSetsOverWhatTheAgentSent()
    {
        var answer = await _host.AnswerAsync(Call("add-todo", """{"folder":"/elsewhere","text":"Modal","priority":"2"}"""), Session);

        var result = answer!["result"]!;
        Assert.False(result["isError"]!.GetValue<bool>());
        var text = JsonNode.Parse(result["content"]![0]!["text"]!.GetValue<string>())!;
        Assert.Equal("/work/overworld", text["folder"]!.GetValue<string>());
        Assert.Equal(2, text["priority"]!.GetValue<int>());
    }

    [Fact]
    public async Task AnswersAFailedOperationAsAnErrorWithItsCode()
    {
        var answer = await _host.AnswerAsync(Call("add-todo", """{"text":""}"""), Session);

        Assert.True(answer!["result"]!["isError"]!.GetValue<bool>());
        Assert.Equal("todo.empty: A todo needs words.", answer["result"]!["content"]![0]!["text"]!.GetValue<string>());
    }

    [Fact]
    public async Task RefusesAToolTheCallerDoesNotHave()
    {
        var answer = await _host.AnswerAsync(Call("remove-project", "{}"), Session);

        Assert.True(answer!["result"]!["isError"]!.GetValue<bool>());
    }

    [Theory]
    [InlineData("not json", -32700)]
    [InlineData("42", -32600)]
    [InlineData("""{"jsonrpc":"2.0","id":9,"method":"resources/list"}""", -32601)]
    public async Task AnswersWhatItCannotDoWithAJsonRpcError(string body, int code)
    {
        var answer = await _host.AnswerAsync(body, Session);

        Assert.Equal(code, answer!["error"]!["code"]!.GetValue<int>());
    }

    [Fact]
    public async Task AnswersNothingToANotificationAndEachMessageOfABatch()
    {
        Assert.Null(await _host.AnswerAsync("""{"jsonrpc":"2.0","method":"notifications/initialized"}""", Session));

        var batch = await _host.AnswerAsync("""[{"jsonrpc":"2.0","id":1,"method":"ping"},{"jsonrpc":"2.0","method":"notifications/initialized"},{"jsonrpc":"2.0","id":2,"method":"ping"}]""", Session);

        Assert.Equal(2, batch!.AsArray().Count);
    }

    [Fact]
    public async Task ServesOverHttpOnlyACallerItAdmitted()
    {
        _host.Start();
        var token = _host.Admit(Session);
        using var client = new HttpClient();

        using var anonymous = await client.PostAsync(_host.Endpoint, Body("""{"jsonrpc":"2.0","id":1,"method":"ping"}"""), Token);
        using var stranger = await PostAsync(client, "wrong", """{"jsonrpc":"2.0","id":1,"method":"ping"}""");
        using var known = await PostAsync(client, token, """{"jsonrpc":"2.0","id":1,"method":"ping"}""");

        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, stranger.StatusCode);
        Assert.Equal(HttpStatusCode.OK, known.StatusCode);
        Assert.Contains("\"result\"", await known.Content.ReadAsStringAsync(Token));
    }

    [Fact]
    public async Task ForgetsACallerItReleased()
    {
        _host.Start();
        var token = _host.Admit(Session);
        using var client = new HttpClient();

        _host.Release(token);
        using var released = await PostAsync(client, token, """{"jsonrpc":"2.0","id":1,"method":"ping"}""");

        Assert.Equal(HttpStatusCode.Unauthorized, released.StatusCode);
    }

    [Fact]
    public void GivesEachCallerATokenOfItsOwn()
    {
        Assert.NotEqual(_host.Admit(Session), _host.Admit(Session));
    }

    [Fact]
    public async Task TurnsAwayBrowsersAndAcceptsNotifications()
    {
        _host.Start();
        var token = _host.Admit(Session);
        using var client = new HttpClient();
        using var browser = new HttpRequestMessage(HttpMethod.Post, _host.Endpoint) { Content = Body("{}") };
        browser.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        browser.Headers.Add("Origin", "https://evil.example");

        using var refused = await client.SendAsync(browser, Token);
        using var notified = await PostAsync(client, token, """{"jsonrpc":"2.0","method":"notifications/initialized"}""");

        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, notified.StatusCode);
    }

    private static StringContent Body(string json) => new(json, Encoding.UTF8, "application/json");

    private async Task<HttpResponseMessage> PostAsync(HttpClient client, string token, string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _host.Endpoint) { Content = Body(json) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await client.SendAsync(request, Token);
    }
}
