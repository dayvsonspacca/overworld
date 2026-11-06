using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Presentation.Chats;

namespace Overworld.Apps.Crafter.Presentation.Tests.Chats;

public sealed class ChatSummaryViewModelTest : IDisposable
{
    private readonly Bench _bench = new();

    public void Dispose() => _bench.Dispose();

    [Fact]
    public void EndsWithTheLastLineTheAgentSaid()
    {
        var chat = _bench.Summary(Bench.Chat("Bar", new Prompt("Add it", [], []), new Reply([new Speech("Done.\n\nThe bar shows the branch."), new Thought("ok")], new Finished())));

        Assert.Equal("The bar shows the branch.", chat.LastLine);
    }

    [Fact]
    public void EndsWithThePromptWhileNothingWasSaid()
    {
        var chat = _bench.Summary(Bench.Chat("Bar", new Prompt("Add a status bar", [], [])));

        Assert.Equal("Add a status bar", chat.LastLine);
    }

    [Fact]
    public void CutsALongLine()
    {
        var chat = _bench.Summary(Bench.Chat("Bar", new Prompt(new string('a', 100), [], [])));

        Assert.Equal(60, chat.LastLine.Length);
        Assert.EndsWith("…", chat.LastLine);
    }
}
