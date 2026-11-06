using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Pictures;

namespace Overworld.Apps.Crafter.Tests.Pictures;

public sealed class PictureStoreTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();

    private readonly PictureStore _store;

    public PictureStoreTest() => _store = new PictureStore(_folder.PathTo("pictures"));

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void KeepsAPictureByWhatItHoldsAndFindsItAgain()
    {
        var picture = new Picture("image/jpeg", [1, 2, 3]);

        var name = _store.Keep(picture);

        Assert.Matches(@"^[0-9a-f]{16}\.jpg$", name);
        Assert.Equal(name, _store.Keep(new Picture("image/jpeg", [1, 2, 3])));
        Assert.Equal(picture, _store.Find(name!));
        Assert.Single(Directory.GetFiles(_folder.PathTo("pictures")));
    }

    [Fact]
    public void KeepsNothingItCannotShow()
    {
        Assert.Null(_store.Keep(new Picture("image/tiff", [1])));
        Assert.False(Directory.Exists(_folder.PathTo("pictures")));
    }

    [Fact]
    public void FindsOnlyInsideItsFolder()
    {
        File.WriteAllBytes(_folder.PathTo("outside.png"), [4]);

        Assert.Null(_store.Find("../outside.png"));
        Assert.Null(_store.Find("missing.png"));
        Assert.Equal(_folder.PathTo("pictures", "outside.png"), _store.PathTo("../outside.png"));
    }

    [Theory]
    [InlineData("shot.PNG", "image/png")]
    [InlineData("photo.jpeg", "image/jpeg")]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("anim.gif", "image/gif")]
    [InlineData("notes.txt", null)]
    public void KnowsAPicturesMediaTypeByItsName(string file, string? mediaType) =>
        Assert.Equal(mediaType, PictureStore.MediaTypeOf(file));
}
