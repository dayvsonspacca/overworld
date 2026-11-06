using Overworld.Apps.Crafter.Library;

namespace Overworld.Apps.Crafter.Tests.Library;

public sealed class MirrorTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();
    private readonly Remote _remote;
    private readonly string _mirrored;

    public MirrorTest()
    {
        _remote = new Remote(_folder.Path);
        _mirrored = _folder.PathTo("data", "library");
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _folder.Dispose();

    [Fact]
    public async Task ClonesTheRepositoryTheFirstTime()
    {
        _remote.Skill("commit", """{ "name": "Commit", "description": "Writes commits." }""").Publish();

        var synced = await new Mirror(_mirrored).SyncAsync(_remote.Repository, Token);

        Assert.True(synced.IsSuccess, synced.IsSuccess ? null : synced.Error.Message);
        Assert.True(File.Exists(Path.Combine(_mirrored, "skills", "commit", "skill.json")));
    }

    [Fact]
    public async Task FollowsWhatTheRepositoryHoldsNow()
    {
        _remote.Skill("commit", """{ "description": "Writes commits." }""").Publish();
        var mirror = new Mirror(_mirrored);
        await mirror.SyncAsync(_remote.Repository, Token);

        _remote.Skill("review", """{ "description": "Reviews code." }""").Publish();
        await mirror.SyncAsync(_remote.Repository, Token);

        Assert.Equal(["commit", "review"], mirror.Read().Skills.Select(skill => skill.Id));
    }

    [Fact]
    public async Task ClonesAgainWhenTheRepositoryChanges()
    {
        _remote.Skill("commit", """{ "description": "Writes commits." }""").Publish();
        var mirror = new Mirror(_mirrored);
        await mirror.SyncAsync(_remote.Repository, Token);
        var other = new Remote(_folder.PathTo("other"));
        other.Skill("review", """{ "description": "Reviews code." }""").Publish();

        await mirror.SyncAsync(other.Repository, Token);

        Assert.Equal(["review"], mirror.Read().Skills.Select(skill => skill.Id));
    }

    [Fact]
    public async Task ReportsARepositoryItCannotReach()
    {
        var synced = await new Mirror(_mirrored).SyncAsync(_folder.PathTo("nowhere.git"), Token);

        Assert.Equal("git.failed", synced.Error.Code);
    }

    [Fact]
    public async Task ReadsSkillsByNameAndTellsWhatItCouldNotRead()
    {
        _remote
            .Skill("commit", """{ "name": "Commit", "description": " Writes commits. ", "whenToUse": "Committing" }""")
            .Skill("api", """{ "description": "Designs APIs.", /* comments are fine */ }""")
            .Skill("broken", "not json")
            .Skill("vague", """{ "name": "Vague" }""")
            .Skill("unwritten", """{ "description": "Has no instructions." }""", instructions: null)
            .Publish();
        var mirror = new Mirror(_mirrored);
        await mirror.SyncAsync(_remote.Repository, Token);

        var (skills, problems) = mirror.Read();

        Assert.Equal(["api", "Commit"], skills.Select(skill => skill.Name));
        Assert.Equal(new Skill("commit", "Commit", "Writes commits.", "Committing", Path.Combine(_mirrored, "skills", "commit")), skills[1]);
        Assert.Equal(
            [
                new SkillProblem("skills/broken", "skill.json is missing or not valid JSON"),
                new SkillProblem("skills/unwritten", "instructions.md is missing"),
                new SkillProblem("skills/vague", "skill.json has no description"),
            ],
            problems);
    }

    [Fact]
    public void ReadsNothingBeforeTheFirstSync()
    {
        var (skills, problems) = new Mirror(_mirrored).Read();

        Assert.Empty(skills);
        Assert.Empty(problems);
    }

    [Fact]
    public async Task SavesASkillAndPushesIt()
    {
        _remote.Publish();
        var mirror = new Mirror(_mirrored);
        await mirror.SyncAsync(_remote.Repository, Token);
        Remote.Git(_mirrored, "config", "user.name", "Test");
        Remote.Git(_mirrored, "config", "user.email", "test@example.com");

        var saved = await mirror.SaveAsync(new SkillDraft("review", "Review", "Reviews code.", null, "Read it all.\n\n"), _remote.Repository, Token);

        Assert.True(saved.IsSuccess, saved.IsSuccess ? null : saved.Error.Message);
        var fresh = new Mirror(_folder.PathTo("fresh"));
        await fresh.SyncAsync(_remote.Repository, Token);
        var skill = Assert.Single(fresh.Read().Skills);
        Assert.Equal(new Skill("review", "Review", "Reviews code.", null, Path.Combine(_folder.PathTo("fresh"), "skills", "review")), skill);
        Assert.Equal("Read it all.\n", await File.ReadAllTextAsync(skill.Instructions, Token));
    }

    [Theory]
    [InlineData("Review", "Reviews code.", "Read it.", "skill.id_invalid")]
    [InlineData("review", " ", "Read it.", "skill.incomplete")]
    [InlineData("review", "Reviews code.", "", "skill.incomplete")]
    public async Task RefusesADraftItCannotKeep(string id, string description, string instructions, string code)
    {
        var saved = await new Mirror(_mirrored).SaveAsync(new SkillDraft(id, "Review", description, null, instructions), _remote.Repository, Token);

        Assert.Equal(code, saved.Error.Code);
    }
}
