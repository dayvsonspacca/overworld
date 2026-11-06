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
    public void IsTitledWithItsNameCapitalized()
    {
        Assert.Equal("Dye", new AppIdentity("dye").Title);
    }
}
