using System.Text.Json;
using Overworld.Allay.Conversations;
using Overworld.Bedrock.Json;

namespace Overworld.Allay.Tests.Conversations;

public sealed class ConversationTest
{
    private static readonly Question Scope = new(
        "Which tests should run?",
        "Tests",
        Several: false,
        [new Option("All", "Every project"), new Option("Allay", "Only Allay's")]);

    private static IReadOnlyList<Turn> Conversation() =>
    [
        new Prompt(
            "Run the tests and fix what fails",
            [new FileMention("src/Overworld.Allay/Conversations/Reply.cs"), new Picture("image/png", [1, 2, 3])],
            ["commit"]),
        new Reply(
            [
                new Notice(NoticeKind.Model, "claude-opus-5-5"),
                new Thought("The tests live under tests/."),
                new Plan([new PlanStep("Run the tests", StepState.Doing), new PlanStep("Fix the failures", StepState.Pending)]),
                new Questions("q-1", [Scope]),
                new Permission("p-1", ActKind.Command, "dotnet test", "Runs the suite", [new Allowance("dotnet test", Reach.Folder)]),
                new Act("a-1", ActKind.Command, "dotnet test"),
                new ActResult("a-1", Succeeded: false, "1 failed"),
                new Act("a-2", ActKind.Change, "src/App.cs", new Changes(2, 1)),
                new ActResult("a-2", Succeeded: true, string.Empty),
                new Withdrawn("q-1"),
                new Speech("Fixed the failing test."),
            ],
            new Finished()),
    ];

    [Fact]
    public void ComparesTurnsByWhatTheyHold()
    {
        Assert.Equal(Conversation(), Conversation());
        Assert.NotEqual(new Prompt("Run the tests", [], []), new Prompt("Run the tests", [], ["commit"]));
        Assert.NotEqual(new Picture("image/png", [1]), new Picture("image/png", [2]));
    }

    [Fact]
    public void ReadsBackAConversationWrittenAsJson()
    {
        var json = JsonSerializer.Serialize(Conversation(), BedrockJson.Options);

        var read = JsonSerializer.Deserialize<IReadOnlyList<Turn>>(json, BedrockJson.Options);

        Assert.Equal(Conversation(), read);
    }

    [Fact]
    public void NamesEachKindInTheJson()
    {
        var json = JsonSerializer.Serialize<Moment>(new Act("a-1", ActKind.Command, "dotnet test"), BedrockJson.Options);

        Assert.Contains("\"type\": \"act\"", json);
        Assert.Contains("\"kind\": \"Command\"", json);
    }

    [Theory]
    [MemberData(nameof(Decisions))]
    public void ReadsBackEveryDecision(Decision decision)
    {
        var json = JsonSerializer.Serialize(decision, BedrockJson.Options);

        Assert.Equal(decision, JsonSerializer.Deserialize<Decision>(json, BedrockJson.Options));
    }

    public static TheoryData<Decision> Decisions() =>
    [
        new AllowOnce(),
        new AllowAlways(),
        new Deny("Not now"),
        new Skip(),
        new Answers([new QuestionAnswer(Scope, [Scope.Options[1]]), new QuestionAnswer(Scope, [], "Only the failing one")]),
    ];

    [Theory]
    [MemberData(nameof(Outcomes))]
    public void ReadsBackEveryOutcome(Outcome outcome)
    {
        var json = JsonSerializer.Serialize(new Reply([], outcome), BedrockJson.Options);

        Assert.Equal(new Reply([], outcome), JsonSerializer.Deserialize<Reply>(json, BedrockJson.Options));
    }

    public static TheoryData<Outcome> Outcomes() =>
    [
        new Finished(),
        new Stopped(),
        new CutShort(),
        new Refused("Out of scope"),
        new Failed("Not signed in"),
    ];
}
