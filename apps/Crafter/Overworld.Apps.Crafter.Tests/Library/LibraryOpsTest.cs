using Overworld.Apps.Crafter.Library;
using Overworld.Apps.Crafter.Preferences;
using Overworld.Apps.Crafter.Projects;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Tests.Library;

public sealed class LibraryOpsTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();
    private readonly Remote _remote;
    private readonly PreferencesStore _preferences;
    private readonly Shelf _shelf;
    private readonly SkillLibrary _library;
    private readonly ProjectSkills _skills;
    private readonly string _overworld;

    public LibraryOpsTest()
    {
        _remote = new Remote(_folder.Path);
        _preferences = new PreferencesStore(_folder.PathTo("data"), "/home/someone");
        _shelf = new Shelf(_folder.PathTo("data"));
        _library = new SkillLibrary(_preferences, new Mirror(_folder.PathTo("data", "library")));
        _skills = new ProjectSkills(_shelf, _library);
        _overworld = Directory.CreateDirectory(_folder.PathTo("overworld")).FullName;
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _folder.Dispose();

    private void Seed() => _remote
        .Skill("commit", """{ "name": "Commit", "description": "Writes commits." }""")
        .Skill("review", """{ "name": "Review", "description": "Reviews code." }""")
        .Publish();

    private Task<Result<LibraryState>> LinkAsync(string repository) =>
        new LinkLibraryOp(_library).ExecuteAsync(new LinkLibrary(repository), Token);

    private Task<Result<LibraryState>> ShowAsync() =>
        new ShowLibraryOp(_library).ExecuteAsync(new ShowLibrary(), Token);

    private Task<Result<LibraryState>> DefaultAsync(string id, bool isDefault = true) =>
        new SetDefaultSkillOp(_library).ExecuteAsync(new SetDefaultSkill(id, isDefault), Token);

    private Task<Result<IReadOnlyList<Skill>>> GiveAsync(string skill, bool given = true) =>
        new GiveSkillOp(_skills).ExecuteAsync(new GiveSkill(_overworld, skill, given), Token);

    private Task<Result<IReadOnlyList<Skill>>> ProjectSkillsAsync() =>
        new ListProjectSkillsOp(_skills).ExecuteAsync(new ListProjectSkills(_overworld), Token);

    [Fact]
    public async Task ShowsNoSkillsBeforeALibraryIsLinked()
    {
        var state = await ShowAsync();

        Assert.Null(state.Value.Repository);
        Assert.Empty(state.Value.Skills);
        Assert.Empty(state.Value.Defaults);
    }

    [Fact]
    public async Task LinksARepositoryAndReadsItsSkills()
    {
        Seed();

        var linked = await LinkAsync($"  {_remote.Repository}  ");

        Assert.Equal(_remote.Repository, linked.Value.Repository);
        Assert.Equal(["commit", "review"], linked.Value.Skills.Select(skill => skill.Id));
        Assert.Equal(["commit", "review"], (await ShowAsync()).Value.Skills.Select(skill => skill.Id));
    }

    [Fact]
    public async Task UnlinksWithABlankRepository()
    {
        Seed();
        await LinkAsync(_remote.Repository);

        var unlinked = await LinkAsync(" ");

        Assert.Null(unlinked.Value.Repository);
        Assert.Empty(unlinked.Value.Skills);
    }

    [Fact]
    public async Task KeepsTheLinkWhenTheRepositoryCannotBeReached()
    {
        var linked = await LinkAsync(_folder.PathTo("nowhere.git"));

        Assert.Equal("git.failed", linked.Error.Code);
        Assert.Equal(_folder.PathTo("nowhere.git"), (await ShowAsync()).Value.Repository);
    }

    [Fact]
    public async Task RefusesToSyncWithoutALibrary()
    {
        var synced = await new SyncLibraryOp(_library).ExecuteAsync(new SyncLibrary(), Token);

        Assert.Equal("library.unlinked", synced.Error.Code);
    }

    [Fact]
    public async Task RefusesToSaveASkillWithoutALibrary()
    {
        var saved = await new SaveSkillOp(_library).ExecuteAsync(new SaveSkill("review", "Review", "Reviews code.", "Read it all."), Token);

        Assert.Equal("library.unlinked", saved.Error.Code);
    }

    [Fact]
    public async Task MakesASkillOneEveryProjectGets()
    {
        Seed();
        await LinkAsync(_remote.Repository);

        Assert.Equal(["review"], (await DefaultAsync("review")).Value.Defaults);
        Assert.Empty((await DefaultAsync("review", isDefault: false)).Value.Defaults);
    }

    [Fact]
    public async Task RefusesToMakeAMissingSkillADefault()
    {
        Seed();
        await LinkAsync(_remote.Repository);

        Assert.Equal("skill.missing", (await DefaultAsync("deploy")).Error.Code);
    }

    [Fact]
    public async Task GivesAProjectTheDefaultsAndTheSkillsGivenToIt()
    {
        Seed();
        await LinkAsync(_remote.Repository);
        await _shelf.AddAsync(_overworld, Token);
        await DefaultAsync("commit");

        Assert.Equal(["commit"], (await ProjectSkillsAsync()).Value.Select(skill => skill.Id));
        Assert.Equal(["commit", "review"], (await GiveAsync("review")).Value.Select(skill => skill.Id));
        Assert.Equal([new Shelved(_overworld, ["review"])], (await _shelf.ListAsync(Token)).Value, new ShelvedComparer());
        Assert.Equal(["commit"], (await GiveAsync("review", given: false)).Value.Select(skill => skill.Id));
        Assert.Null((await _shelf.ListAsync(Token)).Value[0].Skills);
    }

    [Fact]
    public async Task RefusesToGiveASkillToAProjectThatIsNotOpenOrASkillTheLibraryLacks()
    {
        Seed();
        await LinkAsync(_remote.Repository);

        Assert.Equal("project.not_open", (await GiveAsync("review")).Error.Code);
        await _shelf.AddAsync(_overworld, Token);
        Assert.Equal("skill.missing", (await GiveAsync("deploy")).Error.Code);
    }

    [Fact]
    public async Task ReadsTheSkillsCrafterGaveAProject()
    {
        Directory.CreateDirectory(_folder.PathTo("data"));
        await File.WriteAllTextAsync(
            _folder.PathTo("data", "projects.json"),
            $$"""[{ "folder": "{{_overworld}}", "lastOpened": "2026-11-06T10:02:13-03:00", "skills": ["review"] }]""",
            Token);
        Seed();
        await LinkAsync(_remote.Repository);

        Assert.Equal(["review"], (await ProjectSkillsAsync()).Value.Select(skill => skill.Id));
    }

    private sealed class ShelvedComparer : IEqualityComparer<Shelved>
    {
        public bool Equals(Shelved? x, Shelved? y) =>
            x?.Folder == y?.Folder && (x?.Skills ?? []).SequenceEqual(y?.Skills ?? []);

        public int GetHashCode(Shelved obj) => obj.Folder.GetHashCode(StringComparison.Ordinal);
    }
}
