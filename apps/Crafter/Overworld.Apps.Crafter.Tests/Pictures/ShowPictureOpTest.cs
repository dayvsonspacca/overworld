using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Pictures;

namespace Overworld.Apps.Crafter.Tests.Pictures;

public sealed class ShowPictureOpTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();

    private readonly PictureStore _store;

    private readonly string _project;

    public ShowPictureOpTest()
    {
        _store = new PictureStore(_folder.PathTo("pictures"));
        _project = Directory.CreateDirectory(_folder.PathTo("project")).FullName;
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _folder.Dispose();

    [Fact]
    public async Task KeepsACopyOfThePictureAndSaysWhereItIs()
    {
        Directory.CreateDirectory(Path.Combine(_project, "docs"));
        File.WriteAllBytes(Path.Combine(_project, "docs", "mockup.png"), [1, 2, 3]);

        var shown = await new ShowPictureOp(_store).ExecuteAsync(new ShowPicture(_project, "docs/mockup.png", "  The new modal  "), Token);

        Assert.Equal("mockup.png", shown.Value.File);
        Assert.Equal("The new modal", shown.Value.Caption);
        Assert.Equal(new Picture("image/png", [1, 2, 3]), _store.Find(shown.Value.ShownPicture));
    }

    [Fact]
    public async Task ShowsOnlyPictures()
    {
        File.WriteAllText(Path.Combine(_project, "notes.txt"), "hi");

        var shown = await new ShowPictureOp(_store).ExecuteAsync(new ShowPicture(_project, "notes.txt"), Token);

        Assert.Equal("picture.unsupported", shown.Error.Code);
    }

    [Fact]
    public async Task SaysWhenThereIsNoSuchFile()
    {
        var shown = await new ShowPictureOp(_store).ExecuteAsync(new ShowPicture(_project, "/nowhere/shot.png"), Token);

        Assert.Equal("picture.not_found", shown.Error.Code);
    }
}
