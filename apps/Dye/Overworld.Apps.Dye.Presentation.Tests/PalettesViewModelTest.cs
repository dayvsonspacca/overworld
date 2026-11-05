using Overworld.Apps.Dye.Palettes;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Presentation.Tests;

public sealed class PalettesViewModelTest : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("dye-").FullName;

    private readonly PaletteStore _store;

    private readonly PalettesViewModel _palettes;

    public PalettesViewModelTest()
    {
        _store = new PaletteStore(_folder);
        _palettes = new PalettesViewModel(Operation.From(new ListPalettesOp(_store)), Operation.From(new SavePaletteOp(_store)));
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public async Task ShowsNothingSelectedWhenThereAreNoPalettes()
    {
        await _palettes.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Empty(_palettes.Palettes);
        Assert.Null(_palettes.Selected);
        Assert.Null(_palettes.Problem);
    }

    [Fact]
    public async Task LoadsEveryPaletteAndSelectsTheFirst()
    {
        await Save("Sea", "#003F5C");
        await Save("Fire", "#FF6347");

        await _palettes.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["Sea", "Fire"], _palettes.Palettes.Select(palette => palette.Name));
        Assert.Equal("Sea", _palettes.Selected?.Name);
    }

    [Fact]
    public async Task ReplacesWhatItShowedWhenLoadedAgain()
    {
        await Save("Sea", "#003F5C");
        await _palettes.LoadAsync(TestContext.Current.CancellationToken);

        await Save("Fire", "#FF6347");
        await _palettes.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["Sea", "Fire"], _palettes.Palettes.Select(palette => palette.Name));
    }

    [Fact]
    public async Task AnnouncesWhenTheSelectionChanges()
    {
        await Save("Sea", "#003F5C");
        await Save("Fire", "#FF6347");
        await _palettes.LoadAsync(TestContext.Current.CancellationToken);
        var changed = new List<string?>();
        _palettes.PropertyChanged += (_, change) => changed.Add(change.PropertyName);

        _palettes.Selected = _palettes.Palettes[1];

        Assert.Contains(nameof(PalettesViewModel.Selected), changed);
    }

    [Fact]
    public async Task ShowsTheSwatchesOfTheSelectedPalette()
    {
        await Save("Sea", "#003F5C", "#E4E2DC");
        await Save("Fire", "#FF6347");
        await _palettes.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["003F5C", "E4E2DC"], _palettes.Swatches.Select(swatch => swatch.Label));

        var changed = new List<string?>();
        _palettes.PropertyChanged += (_, change) => changed.Add(change.PropertyName);
        _palettes.Selected = _palettes.Palettes[1];

        Assert.Equal(["FF6347"], _palettes.Swatches.Select(swatch => swatch.Label));
        Assert.Contains(nameof(PalettesViewModel.Swatches), changed);
    }

    [Fact]
    public void ShowsNoSwatchesWithoutASelection()
    {
        Assert.Empty(_palettes.Swatches);
    }

    [Fact]
    public async Task ListsAndSelectsAPaletteSavedFromTheForm()
    {
        await Save("Sea", "#003F5C");
        await _palettes.LoadAsync(TestContext.Current.CancellationToken);
        var listed = new TaskCompletionSource();
        _palettes.PropertyChanged += (_, change) =>
        {
            if (change.PropertyName == nameof(PalettesViewModel.Selected) && _palettes.Selected?.Name == "Fire")
            {
                listed.TrySetResult();
            }
        };

        _palettes.New.Name = "Fire";
        _palettes.New.Colors = "#FF6347";
        await _palettes.New.SaveCommand.ExecuteAsync(null);
        await listed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(["Sea", "Fire"], _palettes.Palettes.Select(palette => palette.Name));
        Assert.Equal("Fire", _palettes.Selected?.Name);
    }

    [Fact]
    public async Task ShowsTheProblemWhenThePalettesCannotBeListed()
    {
        var palettes = new PalettesViewModel(Operation.From(new FailingListOp()), Operation.From(new SavePaletteOp(_store)));

        await palettes.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal("The palettes are out of reach.", palettes.Problem);
        Assert.Empty(palettes.Palettes);
    }

    [Operation("list", "Fails to list.")]
    private sealed class FailingListOp : IOperation<ListPalettes, IReadOnlyList<Palette>>
    {
        public Task<Result<IReadOnlyList<Palette>>> ExecuteAsync(ListPalettes input, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Failure<IReadOnlyList<Palette>>(new Error("palette.unreachable", "The palettes are out of reach.")));
    }

    private Task<Result<Palette>> Save(string name, params string[] colors) =>
        new SavePaletteOp(_store).ExecuteAsync(new SavePalette(name, colors), TestContext.Current.CancellationToken);
}
