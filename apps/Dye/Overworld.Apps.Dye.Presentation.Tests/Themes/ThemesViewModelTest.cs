using Overworld.Apps.Dye.Presentation.Themes;
using Overworld.Apps.Dye.Themes;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Presentation.Tests.Themes;

public sealed class ThemesViewModelTest
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private sealed class Shelf
    {
        public List<Theme> Themes { get; } =
        [
            new("amethyst", true, new Dictionary<string, string> { ["background"] = "#1C1B21", ["accent"] = "#8E62D1", ["scrim"] = "#9E0B0A0E", ["glow"] = "#FFFFFF" }),
            new("deep-sea", false, new Dictionary<string, string> { ["background"] = "#101418", ["accent"] = "#3A7BD5" }),
        ];

        public Error? Failure { get; set; }

        public ThemesViewModel ViewModel() => new(Operation.From(new ListOp(this)), Operation.From(new ApplyOp(this)));

        [Operation("themes-list", "Lists the themes.")]
        private sealed class ListOp(Shelf shelf) : IOperation<ListThemes, IReadOnlyList<Theme>>
        {
            public Task<Result<IReadOnlyList<Theme>>> ExecuteAsync(ListThemes input, CancellationToken cancellationToken) =>
                Task.FromResult(shelf.Failure is { } failure ? Result.Failure<IReadOnlyList<Theme>>(failure) : Result.Success<IReadOnlyList<Theme>>([.. shelf.Themes]));
        }

        [Operation("themes-apply", "Applies a theme.")]
        private sealed class ApplyOp(Shelf shelf) : IOperation<ApplyTheme, Theme>
        {
            public Task<Result<Theme>> ExecuteAsync(ApplyTheme input, CancellationToken cancellationToken)
            {
                for (var i = 0; i < shelf.Themes.Count; i++)
                {
                    shelf.Themes[i] = shelf.Themes[i] with { Active = shelf.Themes[i].Name == input.Name };
                }

                return Task.FromResult(Result.Success(shelf.Themes.Single(theme => theme.Name == input.Name)));
            }
        }
    }

    [Fact]
    public async Task SelectsTheThemeWornNow()
    {
        var themes = new Shelf().ViewModel();

        await themes.LoadAsync(Token);

        Assert.Equal(["amethyst", "deep-sea"], themes.Themes.Select(theme => theme.Name));
        Assert.Equal("amethyst", themes.Selected!.Name);
        Assert.False(themes.CanApply);
        Assert.False(themes.ApplyCommand.CanExecute(null));
    }

    [Fact]
    public async Task AppliesTheSelectedThemeAndKeepsItSelected()
    {
        var themes = new Shelf().ViewModel();
        await themes.LoadAsync(Token);
        themes.Selected = themes.Themes[1];

        Assert.True(themes.ApplyCommand.CanExecute(null));
        await themes.ApplyCommand.ExecuteAsync(null);

        Assert.Equal("deep-sea", themes.Selected!.Name);
        Assert.True(themes.Selected.Active);
        Assert.False(themes.Themes[0].Active);
    }

    [Fact]
    public async Task GroupsTheColorsByWhatTheyDo()
    {
        var themes = new Shelf().ViewModel();

        await themes.LoadAsync(Token);

        Assert.Equal(["Surfaces", "Accent", "Tints", "Other"], themes.Groups.Select(group => group.Title));
        Assert.True(themes.Groups.Single(group => group.Title == "Tints").Swatches.Single().IsTint);
        Assert.Equal("glow", themes.Groups[^1].Swatches.Single().Role);
    }

    [Fact]
    public async Task OffersTheCommandToSaveAThemeFromTheOneShown()
    {
        var themes = new Shelf().ViewModel();
        await themes.LoadAsync(Token);

        themes.Selected = themes.Themes[1];

        Assert.Contains("--from deep-sea", themes.Saving);
    }

    [Fact]
    public async Task TellsWhatWentWrongWhenThemesCannotBeListed()
    {
        var shelf = new Shelf { Failure = new Error("theme.folder_missing", "There are no themes; run just seed.") };
        var themes = shelf.ViewModel();

        await themes.LoadAsync(Token);

        Assert.Empty(themes.Themes);
        Assert.Null(themes.Selected);
        Assert.Contains("just seed", themes.Problem);
    }

    [Fact]
    public async Task ShowsThemesAfterReloadingThem()
    {
        var shelf = new Shelf();
        var dye = new DyeViewModel(null!, shelf.ViewModel(), null!);
        shelf.Themes.Add(new Theme("ember", false, new Dictionary<string, string>()));

        await dye.ShowThemesCommand.ExecuteAsync(null);

        Assert.True(dye.ShowsThemes);
        Assert.False(dye.ShowsPalettes);
        Assert.Equal(3, dye.Themes.Themes.Count);

        dye.ShowPalettesCommand.Execute(null);
        Assert.True(dye.ShowsPalettes);
    }
}
