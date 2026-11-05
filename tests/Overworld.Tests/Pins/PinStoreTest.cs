using Overworld.Pins;

namespace Overworld.Tests.Pins;

public sealed class PinStoreTest : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("pins-").FullName;

    private string PathTo(string name) => Path.Combine(_folder, name);

    private PinStore Store() => new(PathTo("menu.json"));

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public async Task ListsNothingBeforeAnythingIsPinned()
    {
        Assert.Empty((await Store().ListAsync(Token)).Value);
    }

    [Fact]
    public async Task KeepsPinsInTheOrderTheyCameAcrossRestarts()
    {
        await Store().AddAsync("firefox", Token);
        var pins = await Store().AddAsync("kitty", Token);

        Assert.Equal(["firefox", "kitty"], pins.Value);
        Assert.Equal(["firefox", "kitty"], (await Store().ListAsync(Token)).Value);
    }

    [Fact]
    public async Task PinsAnAppOnlyOnce()
    {
        await Store().AddAsync("firefox", Token);

        Assert.Equal(["firefox"], (await Store().AddAsync("firefox", Token)).Value);
    }

    [Fact]
    public async Task UnpinsAnApp()
    {
        await Store().AddAsync("firefox", Token);
        await Store().AddAsync("kitty", Token);

        Assert.Equal(["kitty"], (await Store().RemoveAsync("firefox", Token)).Value);
    }

    [Fact]
    public async Task WritesNothingWhenNothingChanges()
    {
        await Store().ListAsync(Token);
        await Store().RemoveAsync("firefox", Token);

        Assert.False(File.Exists(PathTo("menu.json")));
    }

    [Fact]
    public async Task LeavesAFileItCannotReadUntouched()
    {
        await File.WriteAllTextAsync(PathTo("menu.json"), "{ not json", Token);

        var pins = await Store().AddAsync("firefox", Token);

        Assert.Equal("file.unreadable", pins.Error.Code);
        Assert.Equal("{ not json", await File.ReadAllTextAsync(PathTo("menu.json"), Token));
    }

    [Fact]
    public async Task ReadsAmethystsPinFiles()
    {
        await File.WriteAllTextAsync(PathTo("menu.json"), "[\n  \"firefox\",\n  \"org.gnome.Nautilus\"\n]", Token);

        Assert.Equal(["firefox", "org.gnome.Nautilus"], (await Store().ListAsync(Token)).Value);
    }
}
