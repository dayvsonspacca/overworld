using Overworld.Beacon;
using Overworld.Daemon.Pins;

namespace Overworld.Daemon.Tests.Pins;

public sealed class PinOpsTest : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("pins-").FullName;

    private readonly PinPlaces _places;

    private readonly Beam _beam = new();

    public PinOpsTest() => _places = new PinPlaces(new Dictionary<string, PinStore>
    {
        ["menu"] = new(Path.Combine(_folder, "menu.json")),
        ["taskbar"] = new(Path.Combine(_folder, "taskbar.json")),
    });

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public async Task KeepsEachPlaceApart()
    {
        await new AddPinOp(_places, _beam).ExecuteAsync(new AddPin("taskbar", "kitty"), Token);
        await new AddPinOp(_places, _beam).ExecuteAsync(new AddPin("menu", "firefox"), Token);

        Assert.Equal(["kitty"], (await new ListPinsOp(_places).ExecuteAsync(new ListPins("taskbar"), Token)).Value);
        Assert.Equal(["firefox"], (await new ListPinsOp(_places).ExecuteAsync(new ListPins("menu"), Token)).Value);
    }

    [Fact]
    public async Task TrimsTheAppAndThePlace()
    {
        var pins = await new AddPinOp(_places, _beam).ExecuteAsync(new AddPin(" taskbar ", "  kitty "), Token);

        Assert.Equal(["kitty"], pins.Value);
    }

    [Fact]
    public async Task UnpinsFromThePlaceGiven()
    {
        await new AddPinOp(_places, _beam).ExecuteAsync(new AddPin("menu", "firefox"), Token);

        var pins = await new RemovePinOp(_places, _beam).ExecuteAsync(new RemovePin("menu", "firefox"), Token);

        Assert.Empty(pins.Value);
    }

    [Theory]
    [InlineData("desktop")]
    [InlineData("")]
    public async Task RefusesAPlaceItDoesNotHave(string place)
    {
        var listed = await new ListPinsOp(_places).ExecuteAsync(new ListPins(place), Token);
        var added = await new AddPinOp(_places, _beam).ExecuteAsync(new AddPin(place, "firefox"), Token);

        Assert.Equal("pins.unknown_place", listed.Error.Code);
        Assert.Equal("pins.unknown_place", added.Error.Code);
        Assert.Contains("menu or taskbar", listed.Error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RefusesAPinWithoutAnApp(string app)
    {
        var added = await new AddPinOp(_places, _beam).ExecuteAsync(new AddPin("menu", app), Token);
        var removed = await new RemovePinOp(_places, _beam).ExecuteAsync(new RemovePin("menu", app), Token);

        Assert.Equal("pins.app_empty", added.Error.Code);
        Assert.Equal("pins.app_empty", removed.Error.Code);
    }

    [Fact]
    public async Task SignalsPinsWhenAPlaceChanges()
    {
        await using var watcher = _beam.WatchAsync(Token).GetAsyncEnumerator(Token);

        var added = watcher.MoveNextAsync();
        await new AddPinOp(_places, _beam).ExecuteAsync(new AddPin("taskbar", "kitty"), Token);
        Assert.True(await added);
        Assert.Equal("pins", watcher.Current);

        var removed = watcher.MoveNextAsync();
        await new RemovePinOp(_places, _beam).ExecuteAsync(new RemovePin("taskbar", "kitty"), Token);
        Assert.True(await removed);
        Assert.Equal("pins", watcher.Current);
    }
}
