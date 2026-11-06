using Overworld.Allay.ClaudeCode.Protocol;
using Overworld.Allay.Conversations;

namespace Overworld.Allay.ClaudeCode.Tests.Protocol;

public sealed class MessagesTest
{
    private static readonly Question Which = new("Which?", "Pick", true, [new Option("A", ""), new Option("B", "")]);

    [Fact]
    public void AsksWithTextAsPeopleTypedIt()
    {
        Assert.Equal(
            """{"type":"user","uuid":"u-1","message":{"role":"user","content":"Olá <b> & \"tu\""}}""",
            Messages.Asking("Olá <b> & \"tu\"", [], "u-1"));
    }

    [Fact]
    public void AsksWithPicturesAheadOfTheText()
    {
        Assert.Equal(
            """{"type":"user","message":{"role":"user","content":[{"type":"image","source":{"type":"base64","media_type":"image/png","data":"AQI="}},{"type":"text","text":"See"}]}}""",
            Messages.Asking("See", [new Picture("image/png", [1, 2])]));
    }

    [Fact]
    public void AllowsWithTheInputAsItWas()
    {
        var answer = Messages.Answering("r-1", new AllowOnce(), """{"command":"ls"}""", "[]");

        Assert.Contains("\"behavior\":\"allow\",\"updatedInput\":{\"command\":\"ls\"}", answer);
        Assert.DoesNotContain("updatedPermissions", answer);
    }

    [Fact]
    public void AllowsAlwaysWithWhatClaudeCodeSuggested()
    {
        Assert.Contains("\"updatedPermissions\":[{\"type\":\"addRules\"}]", Messages.Answering("r-1", new AllowAlways(), "{}", """[{"type":"addRules"}]"""));
    }

    [Theory]
    [InlineData(false, null, "The user denied this. Don't try it again; take another way or ask them how to proceed.")]
    [InlineData(false, " use mkdir ", "The user denied this and said: use mkdir")]
    [InlineData(true, null, "The user chose not to answer. Carry on with your best judgment or ask in plain text.")]
    [InlineData(true, "later", "The user chose not to answer and said: later")]
    public void TellsWhyItDenied(bool skipped, string? reason, string message)
    {
        var answer = Messages.Answering("r-1", skipped ? new Skip(reason) : new Deny(reason), "{}", null);

        Assert.Contains("\"behavior\":\"deny\"", answer);
        Assert.Contains($"\"message\":\"{message}\"", answer);
    }

    [Fact]
    public void AnswersQuestionsKeepingTheRestOfTheInput()
    {
        var answer = Messages.Answering(
            "r-1",
            new Answers([new QuestionAnswer(Which, [Which.Options[1]], "and C")]),
            """{"questions":[{"question":"Which?"}],"answers":{"old":"x"}}""",
            null);

        Assert.Contains("\"updatedInput\":{\"questions\":[{\"question\":\"Which?\"}],\"answers\":{\"Which?\":\"B, and C\"}}", answer);
    }
}
