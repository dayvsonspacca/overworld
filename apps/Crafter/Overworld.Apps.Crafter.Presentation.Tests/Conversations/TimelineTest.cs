using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Presentation.Conversations;
using Overworld.Apps.Crafter.Presentation.Markdown;

namespace Overworld.Apps.Crafter.Presentation.Tests.Conversations;

public sealed class TimelineTest
{
    private readonly Timeline _timeline = new();

    [Fact]
    public void ShowsThePictureAnAgentSentRightUnderTheStepThatSentIt()
    {
        var mockup = new Picture("image/png", [1, 2, 3]);
        var timeline = new Timeline(find: name => name == "ab12.png" ? mockup : null);
        timeline.Add(new Act("1", ActKind.Tool, "crafter: show picture · docs/mockup.png"));
        timeline.Add(new Speech("Here it is."));

        timeline.Add(new ActResult("1", true, """{ "shownPicture": "ab12.png", "file": "mockup.png", "caption": "The new modal" }"""));

        var shown = Assert.IsType<PictureEntry>(timeline.Entries[1]);
        Assert.Equal(mockup, shown.Picture.Picture);
        Assert.Equal(("mockup.png", "The new modal"), (shown.File, shown.Caption));
        Assert.IsType<SpeechEntry>(timeline.Entries[2]);
        Assert.Equal(timeline.Entries, timeline.Shown);
    }

    [Theory]
    [InlineData(ActKind.Command, true, """{ "shownPicture": "ab12.png" }""")]
    [InlineData(ActKind.Tool, false, """{ "shownPicture": "ab12.png" }""")]
    [InlineData(ActKind.Tool, true, """{ "shownPicture": "gone.png" }""")]
    [InlineData(ActKind.Tool, true, "{ not json")]
    [InlineData(ActKind.Tool, true, "\"Todo #3 is done.\"")]
    public void ShowsNoPictureForAnyOtherResult(ActKind kind, bool succeeded, string output)
    {
        var timeline = new Timeline(find: name => name == "ab12.png" ? new Picture("image/png", [1]) : null);
        timeline.Add(new Act("1", kind, "something"));

        timeline.Add(new ActResult("1", succeeded, output));

        Assert.IsType<ActEntry>(Assert.Single(timeline.Entries));
    }

    [Fact]
    public void ShowsTheLatestEntriesAndBringsBackEarlierOnesAPageAtATime()
    {
        for (var at = 0; at < (Timeline.Page * 2) + 3; at++)
        {
            _timeline.Add(new Act($"{at}", ActKind.Command, $"step {at}"));
        }

        _timeline.ShowLatest();

        Assert.Equal(_timeline.Entries.TakeLast(Timeline.Page), _timeline.Shown);
        Assert.True(_timeline.HasEarlier);

        Assert.Equal(Timeline.Page, _timeline.ShowEarlier());
        Assert.Equal(_timeline.Entries.TakeLast(Timeline.Page * 2), _timeline.Shown);

        Assert.Equal(3, _timeline.ShowEarlier());
        Assert.Equal(_timeline.Entries, _timeline.Shown);
        Assert.False(_timeline.HasEarlier);
        Assert.Equal(0, _timeline.ShowEarlier());
    }

    [Fact]
    public void ShowsWhatArrivesWhileOnlyTheLatestAreShown()
    {
        for (var at = 0; at < Timeline.Page + 5; at++)
        {
            _timeline.Add(new Act($"{at}", ActKind.Command, $"step {at}"));
        }

        _timeline.ShowLatest();
        _timeline.Add(new Speech("Done."));

        Assert.Equal(Timeline.Page + 1, _timeline.Shown.Count);
        Assert.IsType<SpeechEntry>(_timeline.Shown[^1]);
        Assert.Equal(_timeline.Entries.TakeLast(Timeline.Page + 1), _timeline.Shown);
    }

    [Fact]
    public void JoinsWhatFlowsInPiecesAndSplitsWhenTheKindChanges()
    {
        _timeline.Add(new Thought("Look"));
        _timeline.Add(new Thought("ing."));
        _timeline.Add(new Speech("Hello "));
        _timeline.Add(new Speech("**there**"));

        Assert.Equal("Looking.", Assert.IsType<ThoughtEntry>(_timeline.Entries[0]).Text);
        var speech = Assert.IsType<SpeechEntry>(_timeline.Entries[1]);
        Assert.Equal("Hello **there**", speech.Text);
        Assert.Contains(Assert.IsType<Paragraph>(Assert.Single(speech.Blocks)).Fragments, fragment => fragment is { Text: "there", Look: Look.Bold });
    }

    [Fact]
    public void SettlesAnActWithItsResultAndOpensItOnlyWhenItFailed()
    {
        _timeline.Add(new Act("1", ActKind.Command, "dotnet test"));
        _timeline.Add(new Act("2", ActKind.Change, "src/A.cs", new Changes(3, 1)));
        var command = Assert.IsType<ActEntry>(_timeline.Entries[0]);
        Assert.True(command.IsRunning);

        _timeline.Add(new ActResult("1", false, "Failed RefusesTwoOutcomes"));
        _timeline.Add(new ActResult("2", true, "ok"));

        Assert.Equal(("$", "failed", true), (command.Glyph, command.Verdict, command.ShowsOutput));
        var change = Assert.IsType<ActEntry>(_timeline.Entries[1]);
        Assert.Equal(("+3", "−1", false), (change.Added, change.Removed, change.ShowsOutput));

        change.ToggleCommand.Execute(null);

        Assert.True(change.ShowsOutput);
    }

    [Fact]
    public void CutsALongOutput()
    {
        _timeline.Add(new Act("1", ActKind.Command, "seq 20"));
        _timeline.Add(new ActResult("1", true, string.Join('\n', Enumerable.Range(1, 20))));

        var shown = Assert.IsType<ActEntry>(_timeline.Entries[0]).Shown.Split('\n');

        Assert.Equal(13, shown.Length);
        Assert.Equal("… 8 more lines", shown[^1]);
    }

    [Fact]
    public void KeepsOnePlanPerReplyUpdatedWhereItFirstShowed()
    {
        _timeline.Add(new Plan([new PlanStep("Find", StepState.Doing), new PlanStep("Fix", StepState.Pending)]));
        _timeline.Add(new Speech("Found it."));
        _timeline.Add(new Plan([new PlanStep("Find", StepState.Done), new PlanStep("Fix", StepState.Doing)]));

        var plan = Assert.IsType<PlanEntry>(_timeline.Entries[0]);
        Assert.Equal(2, _timeline.Entries.Count);
        Assert.Equal("1/2", plan.Progress);

        _timeline.Add(new Prompt("Again", [], []));
        _timeline.Add(new Plan([new PlanStep("Redo", StepState.Doing)]));

        Assert.IsType<PlanEntry>(_timeline.Entries[^1]);
        Assert.Equal("1/2", plan.Progress);
    }

    [Fact]
    public void ReplaysARecordedReplyEndingWithItsOutcome()
    {
        _timeline.Add(new Prompt("Fix it", [new FileMention("src/A.cs"), new Picture("image/png", [1])], ["commit"]));
        _timeline.Add(new Reply([new Notice(NoticeKind.Context, "Summarized"), new Speech("Done.")], new Failed("Out of credit")));

        var prompt = Assert.IsType<PromptEntry>(_timeline.Entries[0]);
        Assert.Equal(["src/A.cs"], prompt.Files);
        Assert.Single(prompt.Pictures);
        Assert.Equal(["commit"], prompt.Skills);
        Assert.IsType<NoticeEntry>(_timeline.Entries[1]);
        var outcome = Assert.IsType<OutcomeEntry>(_timeline.Entries[^1]);
        Assert.Equal(("Failed: Out of credit", true), (outcome.Text, outcome.WentWrong));
    }

    [Fact]
    public void IgnoresAResultOfNoActShown()
    {
        _timeline.Add(new ActResult("hidden", true, "plan saved"));

        Assert.Empty(_timeline.Entries);
    }
}
