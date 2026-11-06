using Overworld.Allay.Agents;
using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Pictures;

namespace Overworld.Apps.Crafter.Tests.Conversations;

public sealed class ChatStoreTest : IDisposable
{
    private const string Project = "/home/hero/Projetos/me/overworld";

    private static readonly AgentId Claude = new("claude-code");

    private static readonly AgentId Codex = new("codex");

    private readonly TemporaryFolder _folder = new();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _folder.Dispose();

    private ChatStore Store() => new(_folder.Path, new PictureStore(_folder.PathTo("pictures")));

    private async Task<string> SavedFileAsync()
    {
        await Store().SaveAsync(Project, [new ChatRecord(ChatId.New(), Claude, ChatTitle.Of("Odd"), null, [])], Token);

        return Directory.GetFiles(_folder.Path).Single();
    }

    [Fact]
    public async Task StartsEmptyWithoutAFile()
    {
        Assert.Empty((await Store().LoadAsync(Project, Token)).Value);
    }

    private static Prompt Ask(string text) => new(text, [], []);

    [Fact]
    public async Task LoadsWhatItSaved()
    {
        ChatRecord[] chats =
        [
            new(ChatId.New(), Claude, ChatTitle.Of("Fix reconnect"), "s-1",
            [
                Ask("Fix it"),
                new Reply([new Speech("Done")], new Finished()),
                Ask("More"),
                new Reply([new Speech("Half")], new Stopped()),
                Ask("Long"),
                new Reply([new Speech("Cut")], new CutShort()),
                Ask("Risky"),
                new Reply([], new Refused("cyber")),
                Ask("Broken"),
                new Reply([], new Failed("Not logged in")),
                Ask("Run the tests"),
                new Reply(
                    [
                        new Thought("They live under tests/."),
                        new Plan([new PlanStep("Run them", StepState.Doing), new PlanStep("Fix them", StepState.Pending)]),
                        new Speech("Running them."),
                        new Act("t-1", ActKind.Command, "dotnet test"),
                        new ActResult("t-1", true, "Passed: 12"),
                        new Act("t-2", ActKind.Change, "src/App.cs", new Changes(2, 1)),
                        new ActResult("t-2", false, "no match"),
                        new Act("t-3", ActKind.Tool, "crafter: add todo · Modal"),
                        new Speech("All green."),
                    ],
                    new Finished()),
            ]),
            new(ChatId.New(), Codex, ChatTitle.Untitled, null, []),
        ];

        await Store().SaveAsync(Project, chats, Token);
        var loaded = (await Store().LoadAsync(Project, Token)).Value;

        Assert.Equal(chats, loaded);
    }

    [Fact]
    public async Task WritesStepsCrafterCanStillRead()
    {
        await Store().SaveAsync(
            Project,
            [new ChatRecord(ChatId.New(), Claude, ChatTitle.Of("Run"), null, [Ask("Run"), new Reply([new Speech("Ok."), new Act("t-1", ActKind.Command, "dotnet test"), new ActResult("t-1", false, "1 failed")], new Finished())])],
            Token);

        var json = await File.ReadAllTextAsync(Directory.GetFiles(_folder.Path).Single(), Token);

        Assert.Contains("\"kind\": \"asked\"", json);
        Assert.Contains("\"text\": \"Ok.\"", json);
        Assert.Contains("\"does\": \"shell\"", json);
        Assert.Contains("\"tool\": \"Command\"", json);
        Assert.Contains("\"failed\": true", json);
    }

    [Fact]
    public async Task ReadsTheKindOfAnActCrafterSaved()
    {
        var file = await SavedFileAsync();
        await File.WriteAllTextAsync(
            file,
            """[{"id":"6f0c8a4e-3a51-4c1e-9a52-1d2b3c4d5e6f","agent":"claude-code","title":"Old","turns":[{"kind":"answered","text":"","ending":"finished","steps":[{"kind":"acting","id":"t-1","tool":"Bash","does":"shell","summary":"ls"},{"kind":"acting","id":"t-2","tool":"crafter","does":"served","summary":"add todo · Modal"},{"kind":"acting","id":"t-3","tool":"Write","summary":"b.md"},{"kind":"acting","id":"t-4","tool":"Monitor","summary":""}]}]}]""",
            Token);

        var reply = Assert.IsType<Reply>(Assert.Single(Assert.Single((await Store().LoadAsync(Project, Token)).Value).Turns));

        Assert.Equal(
            [
                new Act("t-1", ActKind.Command, "ls"),
                new Act("t-2", ActKind.Tool, "crafter: add todo · Modal"),
                new Act("t-3", ActKind.Create, "b.md"),
                new Act("t-4", ActKind.Other, "Monitor"),
            ],
            reply.Moments);
    }

    [Fact]
    public async Task KeepsEachProjectApart()
    {
        var store = Store();

        await store.SaveAsync(Project, [new ChatRecord(ChatId.New(), Claude, ChatTitle.Of("Here"), null, [])], Token);

        Assert.Empty((await store.LoadAsync("/home/hero/elsewhere/crafter", Token)).Value);
    }

    [Fact]
    public async Task NamesTheFileAsCrafterDid()
    {
        var file = await SavedFileAsync();

        Assert.Matches(@"^overworld-[0-9a-f]{12}\.json$", Path.GetFileName(file));
    }

    [Fact]
    public async Task ReadsAChatCrafterSaved()
    {
        var file = await SavedFileAsync();
        await File.WriteAllTextAsync(
            file,
            """[{"id":"6f0c8a4e-3a51-4c1e-9a52-1d2b3c4d5e6f","agent":"claude-code","title":"Fix","sessionId":"s-1","turns":[{"kind":"asked","text":"Fix it"},{"kind":"answered","text":"Done","ending":"finished"}]}]""",
            Token);

        var chat = Assert.Single((await Store().LoadAsync(Project, Token)).Value);

        Assert.Equal("s-1", chat.SessionId);
        Assert.Equal([Ask("Fix it"), new Reply([new Speech("Done")], new Finished())], chat.Turns);
    }

    [Fact]
    public async Task ReadsBackAPromptThatOnlyCarriedAPicture()
    {
        var store = Store();
        var chat = new ChatRecord(ChatId.New(), Claude, ChatTitle.Of("Look"), null, [new Prompt(string.Empty, [new Picture("image/png", [1, 2, 3])], []), new Reply([new Speech("I see")], new Finished())]);
        await store.SaveAsync(Project, [chat], Token);

        var read = await Store().LoadAsync(Project, Token);

        Assert.True(read.IsSuccess);
        var turns = Assert.Single(read.Value).Turns;
        var prompt = Assert.IsType<Prompt>(turns[0]);
        Assert.Equal(string.Empty, prompt.Text);
        Assert.Equal(new Picture("image/png", [1, 2, 3]), Assert.Single(prompt.Attachments));
        Assert.Equal(new Reply([new Speech("I see")], new Finished()), turns[1]);
    }

    [Fact]
    public async Task KeepsWhatWentAlongWithAPromptAndEachPictureOnce()
    {
        var picture = new Picture("image/png", [7, 7, 7]);
        Turn[] turns =
        [
            new Prompt("Look at this", [new FileMention("src/A.cs"), picture, picture], ["commit"]),
            new Reply([new Speech("Seen")], new Finished()),
            new Prompt("And again", [picture], []),
        ];
        await Store().SaveAsync(Project, [new ChatRecord(ChatId.New(), Claude, ChatTitle.Of("Pictures"), null, turns)], Token);

        var read = Assert.Single((await Store().LoadAsync(Project, Token)).Value).Turns;

        var first = Assert.IsType<Prompt>(read[0]);
        Assert.Equal([new FileMention("src/A.cs"), picture, picture], first.Attachments);
        Assert.Equal(["commit"], first.Skills);
        Assert.Equal([picture], Assert.IsType<Prompt>(read[2]).Attachments);
        Assert.Single(Directory.GetFiles(_folder.PathTo("pictures")));
    }

    [Fact]
    public async Task LeavesOutAPictureThatIsGone()
    {
        await Store().SaveAsync(Project, [new ChatRecord(ChatId.New(), Claude, ChatTitle.Of("Gone"), null, [new Prompt("This", [new Picture("image/png", [9])], [])])], Token);
        File.Delete(Directory.GetFiles(_folder.PathTo("pictures")).Single());

        var read = await Store().LoadAsync(Project, Token);

        Assert.True(read.IsSuccess);
        Assert.Empty(Assert.IsType<Prompt>(Assert.Single(Assert.Single(read.Value).Turns)).Attachments);
    }

    [Fact]
    public async Task ReadsAnUnknownEndingAsAFailure()
    {
        var file = await SavedFileAsync();
        await File.WriteAllTextAsync(
            file,
            """[{"id":"6f0c8a4e-3a51-4c1e-9a52-1d2b3c4d5e6f","agent":"claude-code","title":"Odd","sessionId":null,"turns":[{"kind":"answered","text":"?","ending":"vanished","detail":null}]}]""",
            Token);

        var turn = Assert.Single(Assert.Single((await Store().LoadAsync(Project, Token)).Value).Turns);

        Assert.Equal(new Reply([new Speech("?")], new Failed("The answer could not be read back.")), turn);
    }

    [Fact]
    public async Task ReadsTheLineCountsOfChatsSavedAsOneLine()
    {
        var file = await SavedFileAsync();
        await File.WriteAllTextAsync(
            file,
            """[{"id":"6f0c8a4e-3a51-4c1e-9a52-1d2b3c4d5e6f","agent":"claude-code","title":"Old","sessionId":null,"turns":[{"kind":"answered","text":"","ending":"finished","detail":null,"steps":[{"kind":"acting","id":"t-1","tool":"Edit","summary":"a.cs","stat":"+2 −1"},{"kind":"acting","id":"t-2","tool":"Write","summary":"b.md","stat":"+3"}]}]}]""",
            Token);

        var reply = Assert.IsType<Reply>(Assert.Single(Assert.Single((await Store().LoadAsync(Project, Token)).Value).Turns));

        Assert.Equal([new Act("t-1", ActKind.Change, "a.cs", new Changes(2, 1)), new Act("t-2", ActKind.Create, "b.md", new Changes(3))], reply.Moments);
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

        var loaded = await Store().LoadAsync(Project, Token);

        Assert.Equal("file.unreadable", loaded.Error.Code);
    }
}
