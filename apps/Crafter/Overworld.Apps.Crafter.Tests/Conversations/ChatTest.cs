using Overworld.Allay.Agents;
using Overworld.Allay.Conversations;
using Overworld.Allay.Settings;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Tests.Fakes;

namespace Overworld.Apps.Crafter.Tests.Conversations;

public sealed class ChatTest
{
    private static readonly AgentId Claude = new("claude-code");

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static Prompt Ask(string text) => new(text, [], []);

    [Fact]
    public async Task IsTitledByItsFirstPrompt()
    {
        var chat = new Chat(ChatId.New(), Claude, ChatTitle.Untitled, new Conversation(new ScriptedSession([new Finished()], [new Finished()]), []));

        await chat.AskAsync(Ask("Cover Paces with tests"), Setup.Default, Token).ToListAsync(Token);
        await chat.AskAsync(Ask("And the bot too"), Setup.Default, Token).ToListAsync(Token);

        Assert.Equal("Cover Paces with tests", chat.Title.Text);
    }

    [Fact]
    public async Task KeepsTheTitleOfARestoredChat()
    {
        Turn[] history = [Ask("oi"), new Reply([new Speech("Olá")], new Finished())];
        var chat = new Chat(ChatId.New(), Claude, ChatTitle.Of("Greetings"), new Conversation(new ScriptedSession([new Finished()]), history));

        await chat.AskAsync(Ask("again"), Setup.Default, Token).ToListAsync(Token);

        Assert.Equal("Greetings", chat.Title.Text);
    }

    [Fact]
    public async Task RecordsWhatItNeedsToComeBack()
    {
        var id = ChatId.New();
        var chat = new Chat(id, Claude, ChatTitle.Untitled, new Conversation(new ScriptedSession([new Speech("Olá"), new Finished()]) { Id = "s-1" }, []));

        await chat.AskAsync(Ask("oi"), Setup.Default, Token).ToListAsync(Token);

        Assert.Equal(new ChatRecord(id, Claude, ChatTitle.Of("oi"), "s-1", [Ask("oi"), new Reply([new Speech("Olá")], new Finished())]), chat.Record());
    }
}
