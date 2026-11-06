using Overworld.Apps.Crafter.Projects;

namespace Overworld.Apps.Crafter.Tests.Projects;

public sealed class ProjectTest
{
    [Fact]
    public void IsNamedAfterItsFolderWithoutSettings()
    {
        Assert.Equal(new Project("/work/overworld", "overworld"), Project.Of("/work/overworld", null));
    }

    [Fact]
    public void IsNamedAsItsSettingsSay()
    {
        var project = Project.Of("/work/overworld", new ProjectSettings("Overworld", ".crafter/icon.png"));

        Assert.Equal(new Project("/work/overworld", "Overworld", ".crafter/icon.png"), project);
    }

    [Fact]
    public void NamesTheRootAfterItsPath()
    {
        Assert.Equal("/", Project.Of("/", null).Name);
    }

    [Fact]
    public void FindsItsIconInsideItsFolder()
    {
        Assert.Equal("/work/overworld/.crafter/icon.png", new Project("/work/overworld", "Overworld", ".crafter/icon.png").IconFile);
    }

    [Fact]
    public void HasNoIconFileWithoutAnIcon()
    {
        Assert.Null(new Project("/work/overworld", "Overworld").IconFile);
    }

    [Theory]
    [InlineData("/work/overworld/", "/work/overworld")]
    [InlineData("/work/overworld", "/work/overworld")]
    [InlineData("/work/./overworld", "/work/overworld")]
    [InlineData("/work/zarb/../overworld", "/work/overworld")]
    public void NormalizesAFolderToItsFullPath(string folder, string normalized)
    {
        Assert.Equal(normalized, Project.Normalize(folder));
    }
}
