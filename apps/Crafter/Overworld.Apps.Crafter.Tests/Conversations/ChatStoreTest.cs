using Overworld.Apps.Crafter.Agents;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Conversations.Replies;

namespace Overworld.Apps.Crafter.Tests.Conversations;

public sealed class ChatStoreTest : IDisposable
{
    private const string Crafter = "/home/hero/Projetos/me/desktop/crafter";

    private static readonly AgentId Claude = new("claude-code");

    private static readonly AgentId Codex = new("codex");

    private readonly TemporaryFolder _folder = new();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _folder.Dispose();

    private async Task<string> SavedFileAsync()
    {
        await new ChatStore(_folder.Path).SaveAsync(Crafter, [new ChatRecord(ChatId.New(), Claude, ChatTitle.Of("Odd"), null, [])], Token);

        return Directory.GetFiles(_folder.Path).Single();
    }

    [Fact]
    public async Task StartsEmptyWithoutAFile()
    {
        Assert.Empty((await new ChatStore(_folder.Path).LoadAsync(Crafter, Token)).Value);
    }

    [Fact]
    public async Task LoadsWhatItSaved()
    {
        ChatRecord[] chats =
        [
            new(ChatId.New(), Claude, ChatTitle.Of("Fix reconnect"), "s-1",
            [
                new Asked(new Prompt("Fix it")),
                new Answered("Done", new Finished()),
                new Asked(new Prompt("More")),
                new Answered("Half", new Stopped()),
                new Asked(new Prompt("Long")),
                new Answered("Cut", new CutShort()),
                new Asked(new Prompt("Risky")),
                new Answered(string.Empty, new Refused("cyber")),
                new Asked(new Prompt("Broken")),
                new Answered(string.Empty, new Failed("Not logged in")),
                new Asked(new Prompt("Run the tests")),
                new Answered("Running them.All green.", new Finished())
                {
                    Steps =
                    [
                        new Wrote("Running them."),
                        new Acting("t-1", new Tool(ToolKind.Shell, "Bash"), "dotnet test"),
                        new Acted("t-1", Failed: false, Output: "Passed: 12"),
                        new Acting("t-2", new Tool(ToolKind.Edit, "Edit"), "src/App.cs", new Changes(2, 1)),
                        new Acted("t-2", Failed: true, Output: "no match"),
                        new Wrote("All green."),
                    ],
                },
            ]),
            new(ChatId.New(), Codex, ChatTitle.Untitled, null, []),
        ];

        await new ChatStore(_folder.Path).SaveAsync(Crafter, chats, Token);
        var loaded = (await new ChatStore(_folder.Path).LoadAsync(Crafter, Token)).Value;

        Assert.Equal(chats.Length, loaded.Count);

        for (var at = 0; at < chats.Length; at++)
        {
            Assert.Equal(chats[at].Id, loaded[at].Id);
            Assert.Equal(chats[at].Agent, loaded[at].Agent);
            Assert.Equal(chats[at].Title, loaded[at].Title);
            Assert.Equal(chats[at].SessionId, loaded[at].SessionId);
            Assert.Equal(chats[at].Turns, loaded[at].Turns);
        }
    }

    [Fact]
    public async Task KeepsEachProjectApart()
    {
        var store = new ChatStore(_folder.Path);

        await store.SaveAsync(Crafter, [new ChatRecord(ChatId.New(), Claude, ChatTitle.Of("Here"), null, [])], Token);

        Assert.Empty((await store.LoadAsync("/home/hero/elsewhere/crafter", Token)).Value);
    }

    [Fact]
    public async Task NamesTheFileAsCrafterDid()
    {
        var file = await SavedFileAsync();

        Assert.Matches(@"^crafter-[0-9a-f]{12}\.json$", Path.GetFileName(file));
    }

    [Fact]
    public async Task ReadsAChatCrafterSaved()
    {
        var file = await SavedFileAsync();
        await File.WriteAllTextAsync(
            file,
            """[{"id":"6f0c8a4e-3a51-4c1e-9a52-1d2b3c4d5e6f","agent":"claude-code","title":"Fix","sessionId":"s-1","turns":[{"kind":"asked","text":"Fix it"},{"kind":"answered","text":"Done","ending":"finished"}]}]""",
            Token);

        var chat = Assert.Single((await new ChatStore(_folder.Path).LoadAsync(Crafter, Token)).Value);

        Assert.Equal("s-1", chat.SessionId);
        Assert.Equal([new Asked(new Prompt("Fix it")), new Answered("Done", new Finished())], chat.Turns);
    }

    [Fact]
    public async Task ReadsAnUnknownEndingAsAFailure()
    {
        var file = await SavedFileAsync();
        await File.WriteAllTextAsync(
            file,
            """[{"id":"6f0c8a4e-3a51-4c1e-9a52-1d2b3c4d5e6f","agent":"claude-code","title":"Odd","sessionId":null,"turns":[{"kind":"answered","text":"?","ending":"vanished","detail":null}]}]""",
            Token);

        var turn = Assert.Single(Assert.Single((await new ChatStore(_folder.Path).LoadAsync(Crafter, Token)).Value).Turns);

        Assert.Equal(new Answered("?", new Failed("The answer could not be read back.")), turn);
    }

    [Fact]
    public async Task ReadsTheLineCountsOfChatsSavedAsOneLine()
    {
        var file = await SavedFileAsync();
        await File.WriteAllTextAsync(
            file,
            """[{"id":"6f0c8a4e-3a51-4c1e-9a52-1d2b3c4d5e6f","agent":"claude-code","title":"Old","sessionId":null,"turns":[{"kind":"answered","text":"","ending":"finished","detail":null,"steps":[{"kind":"acting","id":"t-1","tool":"Edit","summary":"a.cs","stat":"+2 −1"},{"kind":"acting","id":"t-2","tool":"Write","summary":"b.md","stat":"+3"}]}]}]""",
            Token);

        var answered = Assert.IsType<Answered>(Assert.Single(Assert.Single((await new ChatStore(_folder.Path).LoadAsync(Crafter, Token)).Value).Turns));

        Assert.Equal([new Acting("t-1", new Tool(ToolKind.Other, "Edit"), "a.cs", new Changes(2, 1)), new Acting("t-2", new Tool(ToolKind.Other, "Write"), "b.md", new Changes(3))], answered.Steps);
    }

    [Fact]
    public async Task WritesTheShapeCrafterWrote()
    {
        var file = await SavedFileAsync();

        var json = await File.ReadAllTextAsync(file, Token);

        Assert.Contains("\"agent\": \"claude-code\"", json);
        Assert.DoesNotContain("sessionId", json);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""[{"id":"6f0c8a4e-3a51-4c1e-9a52-1d2b3c4d5e6f","agent":"claude-code","title":" ","sessionId":null,"turns":[]}]""")]
    [InlineData("""[{"id":"6f0c8a4e-3a51-4c1e-9a52-1d2b3c4d5e6f","title":"No agent","sessionId":null,"turns":[]}]""")]
    public async Task ReportsAFileItCannotRead(string content)
    {
        var file = await SavedFileAsync();
        await File.WriteAllTextAsync(file, content, Token);

        var loaded = await new ChatStore(_folder.Path).LoadAsync(Crafter, Token);

        Assert.Equal("file.unreadable", loaded.Error.Code);
    }
}
