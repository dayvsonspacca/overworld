using Overworld.Apps.Dye.Palettes;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Presentation.Tests;

public sealed class NewPaletteViewModelTest : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("dye-").FullName;

    private readonly PaletteStore _store;

    private readonly NewPaletteViewModel _form;

    private readonly List<Palette> _saved = [];

    public NewPaletteViewModelTest()
    {
        _store = new PaletteStore(_folder);
        _form = new NewPaletteViewModel(Operation.From(new SavePaletteOp(_store)));
        _form.Saved += (_, palette) => _saved.Add(palette);
    }

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public async Task SavesWhatWasTypedAndClearsTheForm()
    {
        _form.Name = "Sea";
        _form.Colors = "#003f5c, #58508d";

        await _form.SaveCommand.ExecuteAsync(null);

        var saved = Assert.Single(_saved);
        Assert.Equal("Sea", saved.Name);
        Assert.Equal(["#003F5C", "#58508D"], saved.Colors.Select(color => color.Hex));
        Assert.Equal("", _form.Name);
        Assert.Equal("", _form.Colors);
        Assert.False(_form.IsSaving);
        Assert.Single((await _store.ListAsync(TestContext.Current.CancellationToken)).Value);
    }

    [Fact]
    public async Task ShowsATakenNameUnderTheName()
    {
        await new SavePaletteOp(_store).ExecuteAsync(new SavePalette("Sea", ["#003F5C"]), TestContext.Current.CancellationToken);
        _form.Name = "Sea";
        _form.Colors = "#FFFFFF";

        await _form.SaveCommand.ExecuteAsync(null);

        Assert.Equal("A palette named 'Sea' already exists.", _form.NameError);
        Assert.Null(_form.ColorsError);
        Assert.Equal("Sea", _form.Name);
        Assert.Empty(_saved);
    }

    [Theory]
    [InlineData("#003F5C, #zzz", "'#zzz' is not a hex color like #003F5C.")]
    [InlineData("", "A palette needs at least one color.")]
    [InlineData(" , ", "A palette needs at least one color.")]
    public async Task ShowsBadColorsUnderTheColors(string colors, string error)
    {
        _form.Name = "Sea";
        _form.Colors = colors;

        await _form.SaveCommand.ExecuteAsync(null);

        Assert.Equal(error, _form.ColorsError);
        Assert.Null(_form.NameError);
        Assert.Equal(colors, _form.Colors);
    }

    [Fact]
    public async Task ShowsAMissingNameUnderTheName()
    {
        _form.Colors = "#003F5C";

        await _form.SaveCommand.ExecuteAsync(null);

        Assert.Equal("A palette needs a name.", _form.NameError);
    }

    [Fact]
    public async Task ForgetsOldErrorsWhenSavingAgain()
    {
        _form.Name = "Sea";
        await _form.SaveCommand.ExecuteAsync(null);
        Assert.NotNull(_form.ColorsError);

        _form.Colors = "#003F5C";
        await _form.SaveCommand.ExecuteAsync(null);

        Assert.Null(_form.ColorsError);
        Assert.Single(_saved);
    }

    [Fact]
    public async Task ShowsAnErrorItCannotPlaceAsAProblem()
    {
        var form = new NewPaletteViewModel(Operation.From(new BrokenSaveOp())) { Name = "Sea", Colors = "#003F5C" };

        await form.SaveCommand.ExecuteAsync(null);

        Assert.Equal("The disk is full.", form.Problem);
        Assert.Null(form.NameError);
        Assert.Null(form.ColorsError);
    }

    [Fact]
    public async Task ClearEmptiesTheFormAndItsErrors()
    {
        _form.Name = "Sea";
        await _form.SaveCommand.ExecuteAsync(null);

        _form.ClearCommand.Execute(null);

        Assert.Equal("", _form.Name);
        Assert.Null(_form.ColorsError);
    }

    [Operation("save", "Fails to save.")]
    private sealed class BrokenSaveOp : IOperation<SavePalette, Palette>
    {
        public Task<Result<Palette>> ExecuteAsync(SavePalette input, CancellationToken cancellationToken) =>
            Task.FromResult<Result<Palette>>(new Error("disk.full", "The disk is full."));
    }
}
