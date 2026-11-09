using Overworld.Allay.Conversations;
using Overworld.Allay.Settings;
using Overworld.Allay.Tests.Fakes;

namespace Overworld.Allay.Tests.Conversations;

public sealed class RecordingTest
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static readonly Prompt Ask = new("Run the tests", [], []);

    private static readonly Ticking Still = new(TimeSpan.Zero);

    private static async Task<List<Moment>> HeardAsync(IAsyncEnumerable<Moment> moments)
    {
        List<Moment> heard = [];

        await foreach (var moment in moments)
        {
            heard.Add(moment);
        }

        return heard;
    }

    [Fact]
    public async Task RecordsThePromptAndTheReplyWithItsSpeechJoined()
    {
        var conversation = new Conversation(
            new ScriptedSession([new Speech("Run"), new Speech("ning."), new Act("a-1", ActKind.Command, "dotnet test"), new ActResult("a-1", true, "Passed"), new Speech("Done."), new Finished()]),
            [],
            Still);

        var heard = await HeardAsync(conversation.AskAsync(Ask, Setup.Default, Token));

        Assert.Equal(6, heard.Count);
        Assert.Equal(
            [
                Ask,
                new Reply(
                    [new Speech("Running."), new Act("a-1", ActKind.Command, "dotnet test"), new ActResult("a-1", true, "Passed"), new Speech("Done.")],
                    new Finished(),
                    TimeSpan.Zero),
            ],
            conversation.Turns);
    }

    [Fact]
    public async Task KeepsThoughtsApartFromSpeechAndOnlyTheLatestPlan()
    {
        var conversation = new Conversation(
            new ScriptedSession(
            [
                new Thought("Look "),
                new Thought("first."),
                new Plan([new PlanStep("Run", StepState.Doing)]),
                new Speech("Ok."),
                new Plan([new PlanStep("Run", StepState.Done)]),
                new Finished(),
            ]),
            [],
            new Ticking(TimeSpan.FromSeconds(3)));

        await HeardAsync(conversation.AskAsync(Ask, Setup.Default, Token));

        Assert.Equal(
            new Reply([new Thought("Look first.", TimeSpan.FromSeconds(3)), new Speech("Ok."), new Plan([new PlanStep("Run", StepState.Done)])], new Finished(), TimeSpan.FromSeconds(9)),
            conversation.Turns[^1]);
    }

    [Fact]
    public async Task LeavesRequestsAndNoticesOutOfTheRecord()
    {
        var conversation = new Conversation(
            new ScriptedSession(
            [
                new Notice(NoticeKind.Model, "opus"),
                new Permission("p-1", ActKind.Command, "dotnet test", null, []),
                new Withdrawn("p-1"),
                new Speech("Skipped."),
                new Finished(),
            ]),
            [],
            Still);

        var heard = await HeardAsync(conversation.AskAsync(Ask, Setup.Default, Token));

        Assert.Equal(5, heard.Count);
        Assert.Equal(new Reply([new Speech("Skipped.")], new Finished(), TimeSpan.Zero), conversation.Turns[^1]);
    }

    [Fact]
    public async Task CutsALongResultInTheRecordButNotInTheReply()
    {
        var output = new string('x', Conversation.LongestOutput + 10);
        var conversation = new Conversation(
            new ScriptedSession([new Act("a-1", ActKind.Read, "big.log"), new ActResult("a-1", true, output), new Finished()]),
            []);

        var heard = await HeardAsync(conversation.AskAsync(Ask, Setup.Default, Token));

        Assert.Equal(output, Assert.IsType<ActResult>(heard[1]).Output);
        var kept = Assert.IsType<ActResult>(Assert.IsType<Reply>(conversation.Turns[^1]).Moments[1]);
        Assert.Equal(Conversation.LongestOutput + 2, kept.Output.Length);
    }

    [Fact]
    public async Task EndsAReplyThatStopsWithoutAnOutcomeAsAFailure()
    {
        var conversation = new Conversation(new ScriptedSession([new Speech("Half")]), [], Still);

        var heard = await HeardAsync(conversation.AskAsync(Ask, Setup.Default, Token));

        Assert.IsType<Failed>(heard[^1]);
        Assert.Equal(new Reply([new Speech("Half")], new Failed("The agent went silent before finishing its reply."), TimeSpan.Zero), conversation.Turns[^1]);
    }

    [Fact]
    public async Task IgnoresWhatComesAfterTheOutcome()
    {
        var conversation = new Conversation(new ScriptedSession([new Finished(), new Speech("Late")]), []);

        var heard = await HeardAsync(conversation.AskAsync(Ask, Setup.Default, Token));

        Assert.Equal([new Finished()], heard);
    }

    [Fact]
    public async Task RecordsWhatTheAgentSaysWhenItWakesOnItsOwn()
    {
        var session = new ScriptedSession();
        var conversation = new Conversation(session, [Ask], Still);
        IAsyncEnumerable<Moment>? woke = null;
        conversation.Woke += (_, moments) => woke = moments;

        session.Wake([new Speech("The build finished."), new Finished()]);
        await HeardAsync(woke!);

        Assert.Equal([Ask, new Reply([new Speech("The build finished.")], new Finished(), TimeSpan.Zero)], conversation.Turns);
    }
}
