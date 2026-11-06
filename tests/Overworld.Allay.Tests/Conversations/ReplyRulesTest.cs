using Overworld.Allay.Conversations;

namespace Overworld.Allay.Tests.Conversations;

public sealed class ReplyRulesTest
{
    private static readonly Permission Asking = new("p-1", ActKind.Command, "dotnet test", null, []);

    [Fact]
    public void KeepsAWellFormedReply()
    {
        Assert.Empty(ReplyRules.Broken(
        [
            new Speech("Running them."),
            Asking,
            new Act("a-1", ActKind.Command, "dotnet test"),
            new ActResult("a-1", true, "Passed"),
            new Questions("q-1", []),
            new Withdrawn("q-1"),
            new Finished(),
        ]));
    }

    [Theory]
    [MemberData(nameof(BrokenReplies))]
    public void NamesWhatAReplyBreaks(IReadOnlyList<Moment> reply, string broken)
    {
        Assert.Contains(ReplyRules.Broken(reply), rule => rule.Contains(broken, StringComparison.Ordinal));
    }

    public static TheoryData<IReadOnlyList<Moment>, string> BrokenReplies() => new()
    {
        { [new Speech("No end")], "exactly one outcome; this one has 0" },
        { [new Finished(), new Stopped()], "exactly one outcome; this one has 2" },
        { [new Finished(), new Speech("After")], "Nothing comes after" },
        { [new Act("a-1", ActKind.Read, "a.cs"), new Act("a-1", ActKind.Read, "a.cs"), new Finished()], "announced more than once" },
        { [new ActResult("a-1", true, ""), new Finished()], "comes before the act, or without it" },
        { [new Act("a-1", ActKind.Read, "a.cs"), new ActResult("a-1", true, ""), new ActResult("a-1", true, ""), new Finished()], "more than one result" },
        { [Asking, Asking, new Finished()], "made more than once" },
        { [new Withdrawn("q-9"), new Finished()], "without having been made" },
        { [Asking, new Withdrawn("p-1"), new Withdrawn("p-1"), new Finished()], "withdrawn more than once" },
    };
}
