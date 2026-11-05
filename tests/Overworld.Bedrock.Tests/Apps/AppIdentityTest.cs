using Overworld.Bedrock.Apps;

namespace Overworld.Bedrock.Tests.Apps;

public sealed class AppIdentityTest
{
    [Fact]
    public void IsCalledAsAnOverworldCommand()
    {
        Assert.Equal("overworld:dye", new AppIdentity("dye").Command);
    }

    [Fact]
    public void KeepsItsDataInItsOwnFolderUnderOverworld()
    {
        var data = new AppIdentity("dye").DataDirectory;

        Assert.Equal(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "overworld", "dye"),
            data);
        Assert.EndsWith(Path.Combine("overworld", "dye"), data);
    }
}
