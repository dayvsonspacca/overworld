using Overworld.Apps.Crafter.Axioms;
using Overworld.Apps.Crafter.Projects;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Tests.Axioms;

public sealed class AxiomOpsTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();
    private readonly Shelf _shelf;
    private readonly SettingsStore _settings = new();
    private readonly AxiomBook _book;
    private readonly string _overworld;

    public AxiomOpsTest()
    {
        _shelf = new Shelf(_folder.PathTo("data"));
        _book = new AxiomBook(_shelf, _settings);
        _overworld = Directory.CreateDirectory(_folder.PathTo("overworld")).FullName;
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _folder.Dispose();

    private async Task OpenAsync() => await _shelf.AddAsync(_overworld, Token);

    private Task<Result<IReadOnlyList<string>>> ListAsync() =>
        new ListAxiomsOp(_book).ExecuteAsync(new ListAxioms(_overworld), Token);

    private Task<Result<IReadOnlyList<string>>> AddAsync(string axiom) =>
        new AddAxiomOp(_book).ExecuteAsync(new AddAxiom(_overworld, axiom), Token);

    private Task<Result<IReadOnlyList<string>>> RemoveAsync(string axiom) =>
        new RemoveAxiomOp(_book).ExecuteAsync(new RemoveAxiom(_overworld, axiom), Token);

    private async Task<ProjectSettings?> SavedAsync() => (await new SettingsStore().ReadAsync(_overworld, Token)).Value;

    [Fact]
    public async Task ListsNoAxiomsForAProjectWithoutSettings()
    {
        await OpenAsync();

        Assert.Empty((await ListAsync()).Value);
    }

    [Fact]
    public async Task AddsAxiomsAtTheEnd()
    {
        await OpenAsync();

        await AddAsync("Never add comments.");
        var added = await AddAsync("  Name every app's command overworld:<app>.  ");

        Assert.Equal(["Never add comments.", "Name every app's command overworld:<app>."], added.Value);
        Assert.Equal(added.Value, (await ListAsync()).Value);
    }

    [Fact]
    public async Task KeepsTheNameAndIconWhenAddingAnAxiom()
    {
        await OpenAsync();
        await _settings.UpdateAsync(_overworld, _ => Result.Success<ProjectSettings?>(new ProjectSettings("Overworld", ".crafter/icon.png")), Token);

        await AddAsync("Never add comments.");

        var saved = await SavedAsync();
        Assert.Equal("Overworld", saved!.Name);
        Assert.Equal(".crafter/icon.png", saved.Icon);
    }

    [Fact]
    public async Task NamesAProjectAfterItsFolderWhenItsFirstAxiomCreatesItsSettings()
    {
        await OpenAsync();

        await AddAsync("Never add comments.");

        Assert.Equal("overworld", (await SavedAsync())!.Name);
    }

    [Fact]
    public async Task RefusesABlankAxiom()
    {
        await OpenAsync();

        Assert.Equal("axiom.empty", (await AddAsync("  ")).Error.Code);
    }

    [Fact]
    public async Task RefusesAnAxiomTheProjectHolds()
    {
        await OpenAsync();
        await AddAsync("Never add comments.");

        Assert.Equal("axiom.held", (await AddAsync(" Never add comments. ")).Error.Code);
    }

    [Fact]
    public async Task RemovesAnAxiom()
    {
        await OpenAsync();
        await AddAsync("Never add comments.");
        await AddAsync("Prefer short names.");

        var left = await RemoveAsync("Never add comments.");

        Assert.Equal(["Prefer short names."], left.Value);
    }

    [Fact]
    public async Task LeavesNoAxiomsFieldWhenTheLastGoes()
    {
        await OpenAsync();
        await AddAsync("Never add comments.");

        await RemoveAsync("Never add comments.");

        Assert.DoesNotContain("axioms", await File.ReadAllTextAsync(Path.Combine(_overworld, ".crafter", "settings.json"), Token));
    }

    [Fact]
    public async Task RefusesToRemoveAnAxiomTheProjectDoesNotHold()
    {
        await OpenAsync();

        Assert.Equal("axiom.missing", (await RemoveAsync("Never add comments.")).Error.Code);
    }

    [Fact]
    public async Task RefusesAProjectThatIsNotOpen()
    {
        Assert.Equal("project.not_open", (await ListAsync()).Error.Code);
        Assert.Equal("project.not_open", (await AddAsync("Never add comments.")).Error.Code);
    }

    [Fact]
    public async Task WillNotAddOverSettingsItCannotRead()
    {
        await OpenAsync();
        Directory.CreateDirectory(Path.Combine(_overworld, ".crafter"));
        await File.WriteAllTextAsync(Path.Combine(_overworld, ".crafter", "settings.json"), "not json", Token);

        Assert.Equal("file.unreadable", (await AddAsync("Never add comments.")).Error.Code);
        Assert.Equal("file.unreadable", (await ListAsync()).Error.Code);
    }
}
