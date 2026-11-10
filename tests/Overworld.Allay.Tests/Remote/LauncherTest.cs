using Overworld.Allay.Remote;

namespace Overworld.Allay.Tests.Remote;

public sealed class LauncherTest : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("launcher-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public async Task StartsTheCommandWithItsArgumentsAsTheyAre()
    {
        var made = Path.Combine(_folder, "a file with spaces");

        Launcher.Start(["touch", made]);

        for (var tries = 0; !File.Exists(made) && tries < 200; tries++)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        Assert.True(File.Exists(made));
    }
}
