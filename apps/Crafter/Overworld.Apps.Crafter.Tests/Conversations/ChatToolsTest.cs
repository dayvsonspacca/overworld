using Overworld.Allay.Agents;
using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Pictures;
using Overworld.Apps.Crafter.Projects;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Tests.Conversations;

public sealed class ChatToolsTest : IDisposable
{
    private const string Project = "/home/hero/Projetos/me/overworld";

    private static readonly AgentId Claude = new("claude-code");

    private readonly TemporaryFolder _folder = new();

    private readonly ChatStore _store;

    private readonly ChatRecord _modal;

    private readonly ChatRecord _empty = new(ChatId.New(), Claude, ChatTitle.Of("Nothing yet"), null, []);

    public ChatToolsTest()
    {
        _store = new ChatStore(_folder.Path, new PictureStore(_folder.PathTo("pictures")));
        _modal = new ChatRecord(
            ChatId.New(),
            Claude,
            ChatTitle.Of("The modal"),
            "s-1",
            [
                new Prompt("The modal hides behind the hint", [], []),
                new Reply([new Speech("The hint is drawn last."), new Act("t-1", ActKind.Change, "WorkbenchView.axaml"), new Speech("Moved the page after it.")], new Finished()),
                new Prompt("Thanks", [], []),
                new Reply([new Speech("Glad it works.")], new Finished()),
            ]);
        _store.SaveAsync(Project, [_modal, _empty], CancellationToken.None).GetAwaiter().GetResult();
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _folder.Dispose();

    [Fact]
    public async Task ListsTheProjectsChatsWithTheirPrompts()
    {
        var listed = await new ListChatsOp(_store).ExecuteAsync(new ListChats(Project), Token);

        Assert.Equal([new ChatListing(_modal.Id.Value, "The modal", 2), new ChatListing(_empty.Id.Value, "Nothing yet", 0)], listed.Value);
    }

    [Fact]
    public async Task ReadsAChatAsWhatWasSaidAndDone()
    {
        var read = await new ReadChatOp(_store).ExecuteAsync(new ReadChat(Project, _modal.Id.Value), Token);

        Assert.Equal(
            """
            # The modal

            ## User

            The modal hides behind the hint

            ## Agent

            The hint is drawn last.
            - Change: WorkbenchView.axaml
            Moved the page after it.

            ## User

            Thanks

            ## Agent

            Glad it works.
            """.ReplaceLineEndings("\n"),
            read.Value);
    }

    [Fact]
    public async Task SaysWhichChatItCannotFind()
    {
        var read = await new ReadChatOp(_store).ExecuteAsync(new ReadChat(Project, Guid.NewGuid()), Token);

        Assert.Equal("chat.not_found", read.Error.Code);
    }

    [Fact]
    public async Task RetitlesTheChat()
    {
        ChatTitle? given = null;

        var said = await new RenameChatOp(title => given = title).ExecuteAsync(new RenameChat(" Pictures in the conversation "), Token);

        Assert.Equal(ChatTitle.Of("Pictures in the conversation"), given);
        Assert.Equal("The chat is titled \"Pictures in the conversation\" now.", said.Value);
    }

    [Fact]
    public async Task KeepsATitleWithWords()
    {
        var called = false;

        var said = await new RenameChatOp(_ => called = true).ExecuteAsync(new RenameChat("  "), Token);

        Assert.Equal("chat.title_empty", said.Error.Code);
        Assert.False(called);
    }

    [Fact]
    public async Task OpensAChatInAnOpenProjectAndSendsItsFirstMessage()
    {
        var (shelf, folder) = await ShelvedAsync();
        var chat = ChatId.New();
        (string Folder, string? Prompt)? asked = null;

        var said = await new OpenChatOp(shelf, (given, prompt) =>
        {
            asked = (given, prompt);

            return Task.FromResult(Result.Success(chat));
        }).ExecuteAsync(new OpenChat(folder + "/", "  Fix the modal  "), Token);

        Assert.Equal((folder, "Fix the modal"), asked);
        Assert.Equal($"Crafter opened chat {chat.Value} in {folder} and sent it the prompt; the project's agent is working on it.", said.Value);
    }

    [Fact]
    public async Task OpensAnEmptyChatWhenThePromptIsBlank()
    {
        var (shelf, folder) = await ShelvedAsync();
        var chat = ChatId.New();
        string? prompt = "unset";

        var said = await new OpenChatOp(shelf, (_, given) =>
        {
            prompt = given;

            return Task.FromResult(Result.Success(chat));
        }).ExecuteAsync(new OpenChat(folder, "   "), Token);

        Assert.Null(prompt);
        Assert.Equal($"Crafter opened chat {chat.Value} in {folder}; it waits for the user's first message.", said.Value);
    }

    [Fact]
    public async Task RefusesToOpenAChatOutsideAnOpenProject()
    {
        var (shelf, _) = await ShelvedAsync();
        var called = false;

        var said = await new OpenChatOp(shelf, (_, _) =>
        {
            called = true;

            return Task.FromResult(Result.Success(ChatId.New()));
        }).ExecuteAsync(new OpenChat(_folder.PathTo("elsewhere"), "Fix it"), Token);

        Assert.Equal("project.not_open", said.Error.Code);
        Assert.Contains("elsewhere", said.Error.Message);
        Assert.False(called);
    }

    [Fact]
    public async Task SaysWhyTheWindowCouldNotOpenTheChat()
    {
        var (shelf, folder) = await ShelvedAsync();

        var said = await new OpenChatOp(shelf, (_, _) => Task.FromResult<Result<ChatId>>(new Error("crafter.no_agent", "No agent is hired yet.")))
            .ExecuteAsync(new OpenChat(folder), Token);

        Assert.Equal("crafter.no_agent", said.Error.Code);
    }

    private async Task<(Shelf Shelf, string Folder)> ShelvedAsync()
    {
        var shelf = new Shelf(_folder.PathTo("data"));
        var folder = Directory.CreateDirectory(_folder.PathTo("alpha")).FullName;
        await shelf.AddAsync(folder, Token);

        return (shelf, folder);
    }
}
