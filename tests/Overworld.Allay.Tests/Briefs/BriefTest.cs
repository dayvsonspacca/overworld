using Overworld.Allay.Briefs;

namespace Overworld.Allay.Tests.Briefs;

public sealed class BriefTest
{
    private static readonly Skill Commit = new("commit", "Commit", "Writes commits.", "/library/skills/commit/instructions.md");

    private static readonly Skill Review = new("review", "Review", "Reviews code.", "/library/skills/review/instructions.md");

    private static readonly ToolServer Crafter = new("crafter", new Uri("http://127.0.0.1:4000/mcp"), "token", ["add_todo"]);

    private static readonly Brief Before = new("/work/overworld", "Overworld is a desktop.", ["Never add comments."], [Commit], [Crafter]);

    [Fact]
    public void HasNoChangeSinceItself()
    {
        var same = Before with { Rules = ["Never add comments."], Skills = [Commit], Tools = [Crafter with { FreeTools = ["add_todo"] }] };

        Assert.True(same.Since(Before).IsEmpty);
        Assert.Equal(Before, same);
    }

    [Fact]
    public void TellsTheRulesAddedAndDropped()
    {
        var after = Before with { Rules = ["Prefer short names."] };

        var change = after.Since(Before);

        Assert.Equal(["Prefer short names."], change.AddedRules);
        Assert.Equal(["Never add comments."], change.RemovedRules);
        Assert.False(change.IsEmpty);
    }

    [Fact]
    public void TellsANewContextAndAContextTakenAway()
    {
        Assert.Equal("Overworld is a whole desktop.", (Before with { Context = "Overworld is a whole desktop." }).Since(Before).NewContext);
        Assert.Equal(string.Empty, (Before with { Context = null }).Since(Before).NewContext);
        Assert.Null(Before.Since(Before).NewContext);
    }

    [Fact]
    public void TellsTheSkillsAddedAndDropped()
    {
        var change = (Before with { Skills = [Review] }).Since(Before);

        Assert.Equal([Review], change.AddedSkills);
        Assert.Equal([Commit], change.RemovedSkills);
    }

    [Fact]
    public void TellsWhenTheToolsChange()
    {
        Assert.True((Before with { Tools = [] }).Since(Before).ToolsChanged);
        Assert.True((Before with { Tools = [Crafter with { Token = "other" }] }).Since(Before).ToolsChanged);
    }
}
