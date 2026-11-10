using Overworld.Allay.Briefs;
using Overworld.Allay.Conversations;
using Overworld.Allay.Hiring;
using Overworld.Allay.Presentation.Chats;
using Overworld.Allay.Presentation.Conversations;
using Overworld.Allay.Remote;
using Overworld.Allay.Tests.Fakes;
using Overworld.Allay.Visits;

namespace Overworld.Allay.Tests.Chats;

public sealed class ChatViewModelTest : IDisposable
{
    private static readonly Brief Brief = new("/tmp/dye", "You are inside Dye.", [], [], []);

    private readonly string _folder = Directory.CreateTempSubdirectory("allay-chat-").FullName;

    private readonly HeldAgent _agent = new("held");

    private readonly Crew _crew;

    public ChatViewModelTest() => _crew = new Crew([_agent], new StaffFile(_folder));

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public async Task TellsWhoWillAnswerBeforeAnythingIsSent()
    {
        await HireAsync();
        var chat = Chat();

        await chat.LoadAsync(Token);

        Assert.Equal("Agent held", chat.Agent);
        Assert.Equal("#D97757", chat.Color);
        Assert.Empty(_agent.Sessions);
    }

    [Fact]
    public async Task SaysSoWhenNoAgentIsHired()
    {
        var chat = Chat();

        await chat.LoadAsync(Token);

        Assert.Null(chat.Agent);
        Assert.Contains("No agent is hired", chat.Problem);
        Assert.False(chat.SendCommand.CanExecute(null));
    }

    [Fact]
    public async Task OpensASessionBriefedByTheAppAndShowsTheReply()
    {
        var chat = await ReadyAsync();
        chat.Draft = "Make a warm theme";

        var sending = chat.SendCommand.ExecuteAsync(null);
        await Until(() => _agent.Sessions is [{ Heard.Count: 1 }]);
        _agent.Sessions[0].Feed.Writer.TryWrite(new Speech("Here it is"));
        _agent.Sessions[0].Feed.Writer.TryWrite(new Finished());
        await sending;

        Assert.Equal("You are inside Dye.", _agent.Sessions[0].Brief.Context);
        Assert.Equal("Make a warm theme", _agent.Sessions[0].Heard[0].Text);
        Assert.Equal(string.Empty, chat.Draft);
        Assert.Equal("Here it is", Assert.Single(chat.Timeline.Entries.OfType<SpeechEntry>()).Text);
        Assert.False(chat.IsWorking);
        Assert.False(chat.IsEmpty);
    }

    [Fact]
    public async Task KeepsTalkingInTheSameSession()
    {
        var chat = await ReadyAsync();

        await TalkAsync(chat, "One");
        await TalkAsync(chat, "Two");

        Assert.Equal(["One", "Two"], Assert.Single(_agent.Sessions).Heard.Select(prompt => prompt.Text));
    }

    [Fact]
    public async Task StartsOverInANewSessionAndEndsTheOldOne()
    {
        var chat = await ReadyAsync();
        await TalkAsync(chat, "One");

        chat.RestartCommand.Execute(null);
        await TalkAsync(chat, "Two");

        Assert.True(_agent.Sessions[0].Ended);
        Assert.Equal(2, _agent.Sessions.Count);
        Assert.Equal("Two", Assert.Single(chat.Timeline.Entries.OfType<PromptEntry>()).Text);
    }

    [Fact]
    public async Task HandsADecisionBackToTheSession()
    {
        var chat = await ReadyAsync();
        chat.Draft = "Apply it";
        var sending = chat.SendCommand.ExecuteAsync(null);
        await Until(() => _agent.Sessions is [{ Heard.Count: 1 }]);
        _agent.Sessions[0].Feed.Writer.TryWrite(new Permission("r1", ActKind.Command, "overworld:dye themes apply", null, []));
        await Until(() => chat.Timeline.Request("r1") is not null);

        chat.Timeline.Request("r1")!.Settle(new AllowOnce());
        _agent.Sessions[0].Feed.Writer.TryWrite(new Finished());
        await sending;

        Assert.Equal(("r1", (Decision)new AllowOnce()), Assert.Single(_agent.Sessions[0].Decided));
    }

    [Fact]
    public async Task EndsItsSessionWhenTheAppIsDone()
    {
        var chat = await ReadyAsync();
        await TalkAsync(chat, "One");

        chat.End();

        Assert.True(_agent.Sessions[0].Ended);
    }

    [Fact]
    public void OpensAndClosesAndWritesASuggestion()
    {
        var chat = new ChatViewModel(new Lobby(_crew), _ => Task.FromResult(Brief), suggestions: ["Make a warm theme"]);

        chat.ToggleCommand.Execute(null);
        chat.SuggestCommand.Execute(chat.Suggestions[0]);

        Assert.True(chat.IsOpen);
        Assert.Equal("Make a warm theme", chat.Draft);

        chat.CloseCommand.Execute(null);

        Assert.False(chat.IsOpen);
    }

    private ChatViewModel Chat() => new(new Lobby(_crew, new AllayLink(Path.Combine(_folder, "none.sock"))), _ => Task.FromResult(Brief));

    private async Task<ChatViewModel> ReadyAsync()
    {
        await HireAsync();
        var chat = Chat();
        await chat.LoadAsync(Token);

        return chat;
    }

    private async Task HireAsync() => await _crew.HireAsync("held", true, Token);

    private async Task TalkAsync(ChatViewModel chat, string text)
    {
        chat.Draft = text;
        var count = _agent.Sessions.Sum(session => session.Heard.Count);
        var sending = chat.SendCommand.ExecuteAsync(null);
        await Until(() => _agent.Sessions.Sum(session => session.Heard.Count) == count + 1);
        _agent.Sessions[^1].Feed.Writer.TryWrite(new Finished());
        await sending;
    }

    private static async Task Until(Func<bool> condition)
    {
        for (var tries = 0; !condition() && tries < 200; tries++)
        {
            await Task.Delay(10, Token);
        }

        Assert.True(condition());
    }
}
