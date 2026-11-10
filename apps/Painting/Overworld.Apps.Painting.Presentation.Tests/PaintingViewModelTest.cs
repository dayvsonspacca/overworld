using Overworld.Apps.Painting.Artworks;
using Overworld.Bedrock.Operations;

namespace Overworld.Apps.Painting.Presentation.Tests;

public sealed class PaintingViewModelTest : IDisposable
{
    private static readonly DateTimeOffset Morning = new(2026, 11, 7, 9, 0, 0, TimeSpan.Zero);

    private readonly string _folder = Directory.CreateTempSubdirectory("painting-").FullName;

    private readonly ArtworkStore _store;

    private readonly PaintingViewModel _painting;

    public PaintingViewModelTest()
    {
        _store = new ArtworkStore(_folder);
        _painting = new PaintingViewModel(Operation.From(new ListArtworksOp(_store)), Operation.From(new ShowArtworkOp(_store)));
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public async Task IsEmptyWhenNothingHasBeenPainted()
    {
        await _painting.LoadAsync(Token);

        Assert.True(_painting.IsEmpty);
        Assert.True(_painting.IsHome);
        Assert.Null(_painting.Problem);
    }

    [Fact]
    public async Task ShowsTheGroupsFirstWithTheUngroupedLast()
    {
        await Add("Notes", null, Morning.AddHours(3));
        await Add("Buttons", ArtworkGroup.Design, Morning);
        await Add("Modal", ArtworkGroup.Design, Morning.AddHours(1));

        await _painting.LoadAsync(Token);

        Assert.Equal([("Design", "Design", 2), ("none", "Without a group", 1)], _painting.Shelves.Select(shelf => (shelf.Key, shelf.Name, shelf.Count)));
        Assert.Equal(2, _painting.Shelves[0].Previews.Count);
    }

    [Fact]
    public async Task EntersAGroupAndListsEachDesignOnceNewestFirst()
    {
        var buttons = await Add("Buttons", ArtworkGroup.Design, Morning);
        await Add("Modal", ArtworkGroup.Design, Morning.AddHours(1));
        await _store.ChangeAsync(buttons.Id, artwork => artwork.Painted("<p>again</p>", Morning.AddHours(2)), Token);
        await _painting.LoadAsync(Token);

        _painting.EnterCommand.Execute("Design");

        Assert.True(_painting.IsInGroup);
        Assert.Equal("Design", _painting.GroupName);
        Assert.Equal([("Buttons", 2), ("Modal", 1)], _painting.Designs.Select(canvas => (canvas.Title, canvas.Versions)));
    }

    [Fact]
    public async Task OpensADesignAtItsLatestVersionAndWalksItsVersions()
    {
        var buttons = await Add("Buttons", ArtworkGroup.Design, Morning);
        await _store.ChangeAsync(buttons.Id, artwork => artwork.Painted("<p>second</p>", Morning.AddHours(1)), Token);
        await _painting.LoadAsync(Token);

        await _painting.OpenCommand.ExecuteAsync(buttons.Id);

        Assert.Equal(("Buttons", "<p>second</p>", true), (_painting.Title, _painting.Html, _painting.IsNewest));
        Assert.False(_painting.NextCommand.CanExecute(null));

        await _painting.PreviousCommand.ExecuteAsync(null);

        Assert.Equal(("<h1>Buttons</h1>", false), (_painting.Html, _painting.IsNewest));

        await _painting.NewestCommand.ExecuteAsync(null);

        Assert.Equal("<p>second</p>", _painting.Html);
    }

    [Fact]
    public async Task GoesBackUpOneLevelAtATime()
    {
        var buttons = await Add("Buttons", ArtworkGroup.Design, Morning);
        await _painting.LoadAsync(Token);
        await _painting.OpenCommand.ExecuteAsync(buttons.Id);

        _painting.BackCommand.Execute(null);

        Assert.True(_painting.IsInGroup);

        _painting.BackCommand.Execute(null);

        Assert.True(_painting.IsHome);
    }

    [Fact]
    public async Task FollowsTheOpenDesignToItsNewVersion()
    {
        var buttons = await Add("Buttons", ArtworkGroup.Design, Morning);
        await _painting.LoadAsync(Token);
        await _painting.OpenCommand.ExecuteAsync(buttons.Id);

        await _store.ChangeAsync(buttons.Id, artwork => artwork.Painted("<p>second</p>", Morning.AddHours(1)), Token);
        await _painting.LoadAsync(Token);

        Assert.Equal(("<p>second</p>", 2), (_painting.Html, _painting.Open!.Version));
    }

    [Fact]
    public async Task MarksWhatWasPaintedWhileLookingElsewhere()
    {
        var buttons = await Add("Buttons", ArtworkGroup.Design, Morning);
        await _painting.LoadAsync(Token);

        await _store.ChangeAsync(buttons.Id, artwork => artwork.Painted("<p>second</p>", Morning.AddHours(1)), Token);
        await _painting.LoadAsync(Token);

        Assert.Equal(buttons.Id, _painting.Fresh);

        await _painting.OpenCommand.ExecuteAsync(buttons.Id);

        Assert.Null(_painting.Fresh);
    }

    [Fact]
    public async Task SaysWhatWentWrongWhenThePaintingsCannotBeRead()
    {
        Directory.CreateDirectory(Path.Combine(_folder, "paintings"));
        await File.WriteAllTextAsync(Path.Combine(_folder, "paintings", $"{Guid.NewGuid()}.json"), "{ not json", Token);

        await _painting.LoadAsync(Token);

        Assert.False(_painting.IsEmpty);
        Assert.NotNull(_painting.Problem);
    }

    private async Task<Artwork> Add(string title, ArtworkGroup? group, DateTimeOffset painted) =>
        (await _store.AddAsync(Artwork.New(title, group, $"<h1>{title}</h1>", painted), Token)).Value;
}
