using Overworld.Bedrock.Chest;

namespace Overworld.Bedrock.Tests.Chest;

public sealed class JsonFileTest : IDisposable
{
    public sealed record Pin(string App);

    private readonly string _folder = Directory.CreateTempSubdirectory("chest-").FullName;

    private string PathTo(params string[] parts) => Path.Combine([_folder, .. parts]);

    private static JsonFile<List<Pin>> Pins(string path) => new(path, () => []);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public async Task ReadsTheEmptyValueWhenTheFileIsMissing()
    {
        var pins = await Pins(PathTo("pins.json")).ReadAsync(TestContext.Current.CancellationToken);

        Assert.True(pins.IsSuccess);
        Assert.Empty(pins.Value);
    }

    [Fact]
    public async Task GivesEachReadItsOwnEmptyValue()
    {
        var file = Pins(PathTo("pins.json"));

        (await file.ReadAsync(TestContext.Current.CancellationToken)).Value.Add(new Pin("firefox"));

        Assert.Empty((await file.ReadAsync(TestContext.Current.CancellationToken)).Value);
    }

    [Fact]
    public async Task ReadsBackWhatItWrote()
    {
        var file = Pins(PathTo("pins.json"));

        await file.WriteAsync([new Pin("firefox"), new Pin("kitty")], TestContext.Current.CancellationToken);

        Assert.Equal([new Pin("firefox"), new Pin("kitty")], (await file.ReadAsync(TestContext.Current.CancellationToken)).Value);
    }

    [Fact]
    public async Task WritesTheSharedJsonShape()
    {
        var path = PathTo("pins.json");

        await Pins(path).WriteAsync([new Pin("firefox")], TestContext.Current.CancellationToken);

        var json = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
        Assert.Contains("\n", json);
        Assert.Contains("\"app\": \"firefox\"", json);
    }

    [Fact]
    public async Task CreatesTheFolderItWritesInAndLeavesNoTemporaryFile()
    {
        var path = PathTo("deep", "down", "pins.json");

        await Pins(path).WriteAsync([], TestContext.Current.CancellationToken);

        Assert.Equal(["pins.json"], Directory.GetFiles(PathTo("deep", "down")).Select(Path.GetFileName));
    }

    [Fact]
    public async Task ReadsTheEmptyValueWhenTheFileHoldsNull()
    {
        var path = PathTo("pins.json");
        await File.WriteAllTextAsync(path, "null", TestContext.Current.CancellationToken);

        Assert.Empty((await Pins(path).ReadAsync(TestContext.Current.CancellationToken)).Value);
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("""{"app":"firefox"}""")]
    [InlineData("""[{"app":3}]""")]
    public async Task RefusesAFileItCannotRead(string content)
    {
        var path = PathTo("pins.json");
        await File.WriteAllTextAsync(path, content, TestContext.Current.CancellationToken);

        var pins = await Pins(path).ReadAsync(TestContext.Current.CancellationToken);

        Assert.False(pins.IsSuccess);
        Assert.Equal("file.unreadable", pins.Error.Code);
        Assert.StartsWith(path, pins.Error.Message);
    }
}
