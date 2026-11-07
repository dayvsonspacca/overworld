using Overworld.Apps.Dye.Profiles;

namespace Overworld.Apps.Dye.Tests.Profiles;

public sealed class ProfileStoreTest : IDisposable
{
    private readonly TemporaryFolder _root = new();

    private readonly string _face;

    private readonly ProfileStore _store;

    public ProfileStoreTest()
    {
        _face = Path.Combine(_root.Path, ".face");
        _store = new ProfileStore("steve", _face);
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _root.Dispose();

    [Fact]
    public void ReadsNoPictureWhenThereIsNone()
    {
        var profile = _store.Read();

        Assert.Equal("steve", profile.User);
        Assert.Null(profile.Picture);
    }

    [Fact]
    public async Task CopiesThePictureSoTheOriginalCanGo()
    {
        var source = Picture("me.PNG", [1, 2, 3]);

        var set = await _store.SetPictureAsync(source, Token);
        File.Delete(source);

        Assert.Equal(_face, set.Value.Picture);
        Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(_face, Token));
    }

    [Fact]
    public async Task ReplacesThePictureBeforeIt()
    {
        await _store.SetPictureAsync(Picture("old.jpg", [1]), Token);

        await _store.SetPictureAsync(Picture("new.webp", [2]), Token);

        Assert.Equal([2], await File.ReadAllBytesAsync(_face, Token));
        Assert.Single(Directory.EnumerateFiles(_root.Path, ".face*"));
    }

    [Fact]
    public async Task RefusesWhatIsNotThere()
    {
        var set = await _store.SetPictureAsync(Path.Combine(_root.Path, "gone.png"), Token);

        Assert.Equal("profile.picture_not_found", set.Error.Code);
    }

    [Fact]
    public async Task RefusesWhatIsNotAPicture()
    {
        var set = await _store.SetPictureAsync(Picture("notes.txt", [1]), Token);

        Assert.Equal("profile.picture_kind", set.Error.Code);
        Assert.False(File.Exists(_face));
    }

    [Fact]
    public async Task RemovesThePicture()
    {
        await _store.SetPictureAsync(Picture("me.png", [1]), Token);

        var profile = _store.RemovePicture();

        Assert.Null(profile.Picture);
        Assert.False(File.Exists(_face));
    }

    [Fact]
    public void RemovesNothingWhenThereIsNoPicture() => Assert.Null(_store.RemovePicture().Picture);

    private string Picture(string name, byte[] bytes)
    {
        var path = Path.Combine(_root.Path, name);
        File.WriteAllBytes(path, bytes);

        return path;
    }
}
