using Overworld.Seed.Steps;

namespace Overworld.Seed.Tests.Steps;

public sealed class LinkStepTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    private static Task<Bedrock.Results.Result<string>> Link(string source, string target) =>
        new LinkStep(source, target).RunAsync(TestContext.Current.CancellationToken);

    [Fact]
    public async Task LinksAFolderWhereNothingIs()
    {
        var source = _folder.Folder("repo", "hypr");
        var target = _folder.PathTo("config", "hypr");

        var linked = await Link(source, target);

        Assert.True(linked.IsSuccess);
        Assert.Equal(source, new DirectoryInfo(target).LinkTarget);
    }

    [Fact]
    public async Task LinksAFile()
    {
        var source = _folder.File("binary", "bin", "overworld");
        var target = _folder.PathTo("local", "bin", "overworld");

        await Link(source, target);

        Assert.Equal(source, new FileInfo(target).LinkTarget);
        Assert.Equal("binary", await File.ReadAllTextAsync(target, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LeavesALinkThatAlreadyPointsToTheSource()
    {
        var source = _folder.Folder("repo", "hypr");
        var target = _folder.PathTo("config", "hypr");
        await Link(source, target);

        var again = await Link(source, target);

        Assert.True(again.IsSuccess);
        Assert.Contains("already points", again.Value);
    }

    [Fact]
    public async Task RefusesALinkToSomewhereElse()
    {
        var source = _folder.Folder("repo", "hypr");
        var target = _folder.PathTo("config", "hypr");
        Directory.CreateDirectory(_folder.PathTo("config"));
        Directory.CreateSymbolicLink(target, _folder.Folder("elsewhere"));

        var linked = await Link(source, target);

        Assert.Equal("seed.taken", linked.Error.Code);
        Assert.Equal(_folder.PathTo("elsewhere"), new DirectoryInfo(target).LinkTarget);
    }

    [Fact]
    public async Task RefusesARealFolder()
    {
        var source = _folder.Folder("repo", "hypr");
        var target = _folder.Folder("config", "hypr");
        _folder.File("mine", "config", "hypr", "hyprland.conf");

        var linked = await Link(source, target);

        Assert.Equal("seed.taken", linked.Error.Code);
        Assert.True(File.Exists(_folder.PathTo("config", "hypr", "hyprland.conf")));
    }

    [Fact]
    public async Task RefusesARealFile()
    {
        var source = _folder.File("new", "bin", "overworld");
        var target = _folder.File("old", "local", "bin", "overworld");

        var linked = await Link(source, target);

        Assert.Equal("seed.taken", linked.Error.Code);
        Assert.Equal("old", await File.ReadAllTextAsync(target, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RefusesToLinkASourceThatIsMissing()
    {
        var target = _folder.PathTo("local", "bin", "overworld");

        var linked = await Link(_folder.PathTo("bin", "overworld"), target);

        Assert.Equal("seed.missing_source", linked.Error.Code);
        Assert.Null(new FileInfo(target).LinkTarget);
    }
}
