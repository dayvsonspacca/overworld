using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Conversations;

namespace Overworld.Apps.Crafter.Tests.Conversations;

public sealed class AskingTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();

    public AskingTest()
    {
        Directory.CreateDirectory(_folder.PathTo("src", "Shell"));
        File.WriteAllText(_folder.PathTo("src", "Shell", "Bar.qml"), "");
    }

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void PointsAtTheFilesAndFoldersMentionedThatExist()
    {
        var prompt = Asking.Of("Look at @src/Shell/Bar.qml, @src/Shell/ and @nowhere.cs", _folder.Path, [], []).Value;

        Assert.Equal([new FileMention("src/Shell/Bar.qml"), new FileMention("src/Shell/")], prompt.Attachments);
        Assert.Equal("Look at @src/Shell/Bar.qml, @src/Shell/ and @nowhere.cs", prompt.Text);
    }

    [Fact]
    public void CallsTheProjectsSkillsAndKeepsWhatWasAttached()
    {
        var picture = new Picture("image/png", [1, 2]);

        var prompt = Asking.Of("  /commit this, then /review  ", _folder.Path, ["commit"], [picture]).Value;

        Assert.Equal(["commit"], prompt.Skills);
        Assert.Equal([picture], prompt.Attachments);
        Assert.Equal("/commit this, then /review", prompt.Text);
    }

    [Fact]
    public void RefusesToOpenWithACommandThatIsNotTheProjects()
    {
        Assert.Equal("crafter.unknown_command", Asking.Of("/compact", _folder.Path, ["commit"], []).Error.Code);
    }

    [Fact]
    public void RefusesNothingAtAll()
    {
        Assert.Equal("crafter.empty_prompt", Asking.Of("   ", _folder.Path, [], []).Error.Code);
        Assert.True(Asking.Of("", _folder.Path, [], [new FileMention("/tmp/a.log")]).IsSuccess);
    }
}
