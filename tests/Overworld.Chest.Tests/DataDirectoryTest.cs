using Overworld.Chest;

namespace Overworld.Chest.Tests;

public sealed class DataDirectoryTest
{
    [Fact]
    public void KeepsEachNameInItsOwnFolderUnderOverworld()
    {
        Assert.Equal(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "overworld", "dye"),
            DataDirectory.For("dye"));
    }
}
