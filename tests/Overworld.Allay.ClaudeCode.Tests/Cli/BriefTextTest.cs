using Overworld.Allay.Briefs;
using Overworld.Allay.ClaudeCode.Cli;
using Overworld.Allay.Conversations;

namespace Overworld.Allay.ClaudeCode.Tests.Cli;

public sealed class BriefTextTest
{
    private static readonly Skill Commit = new("commit", "Commit", "Writes\ncommits.", "/library/skills/commit/instructions.md");

    private static readonly Brief Empty = new("/work/overworld", null, [], [], []);

    [Fact]
    public void SaysNothingWhenThereIsNothingToBrief()
    {
        Assert.Null(BriefText.System(Empty, null));
        Assert.Null(BriefText.System(Empty with { Tools = [new ToolServer("crafter", new Uri("http://127.0.0.1:1/mcp"), "t", [])] }, null));
    }

    [Fact]
    public void BriefsTheContextTheSkillsAndTheRulesInThatOrder()
    {
        var text = BriefText.System(Empty with { Context = "Overworld is a desktop.", Rules = ["Never add\ncomments."], Skills = [Commit] }, null)!;

        Assert.Contains("CLAUDE.md and AGENTS.md files and its memory are turned off", text);
        Assert.Contains("Overworld is a desktop.", text);
        Assert.Contains("- commit (Commit): Writes commits. Instructions: /library/skills/commit/instructions.md", text);
        Assert.Contains("- Never add comments.", text);
        Assert.True(text.IndexOf("# Context", StringComparison.Ordinal) < text.IndexOf("# Skills", StringComparison.Ordinal));
        Assert.True(text.IndexOf("# Skills", StringComparison.Ordinal) < text.IndexOf("# Rules", StringComparison.Ordinal));
    }

    [Fact]
    public void PointsAtTheShelvedInstructionsOfASkill()
    {
        var shelf = new SkillShelf("/shelf", [Commit]);

        Assert.Contains("Instructions: /shelf/commit/instructions.md", BriefText.System(Empty with { Skills = [Commit] }, shelf));
    }

    [Fact]
    public void UpdatesWithOnlyWhatChanged()
    {
        var before = Empty with { Context = "Old.", Rules = ["Never add comments."] };
        var after = before with { Context = "New.", Rules = ["Prefer short names."], Skills = [Commit] };

        var update = BriefText.Update(after.Since(before), null)!;

        Assert.StartsWith("<brief-update>", update);
        Assert.EndsWith("</brief-update>", update);
        Assert.Contains("- New rule: Prefer short names.", update);
        Assert.Contains("- Rule dropped: Never add comments.", update);
        Assert.Contains("- New skill: commit (Commit)", update);
        Assert.Contains("The context is now:\n\nNew.", update);
    }

    [Fact]
    public void SaysWhenTheContextWasTakenAway()
    {
        var before = Empty with { Context = "Old." };

        Assert.Contains("The context was taken away.", BriefText.Update(Empty.Since(before), null));
    }

    [Fact]
    public void HasNoUpdateForNoChangeNorForToolsAlone()
    {
        Assert.Null(BriefText.Update(Empty.Since(Empty), null));
        Assert.Null(BriefText.Update((Empty with { Tools = [new ToolServer("crafter", new Uri("http://127.0.0.1:1/mcp"), "t", [])] }).Since(Empty), null));
    }

    [Fact]
    public void PointsAtTheFilesTheUserMentioned()
    {
        Assert.Equal("Fix it", BriefText.Pointing("Fix it", [new Picture("image/png", [1])]));
        Assert.Equal(
            "Fix it\n\n<files>\nThe user points at these files:\n- src/App.cs\n- README.md\n</files>",
            BriefText.Pointing("Fix it", [new FileMention("src/App.cs"), new FileMention("README.md")]));
    }

    [Fact]
    public void CallsOnlySkillsItKnowsWithInstructionsOnDisk()
    {
        Assert.Equal("Fix it", BriefText.Calling("Fix it", ["commit", "unknown"], [Commit], null));
    }
}
