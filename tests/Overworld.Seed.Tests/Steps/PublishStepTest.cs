using Overworld.Seed.Steps;

namespace Overworld.Seed.Tests.Steps;

public sealed class PublishStepTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    [Fact]
    public async Task KeepsWhatWasPublishedWhenTheProjectHasNotChanged()
    {
        _folder.File("<Project />", "App", "App.csproj");
        var fingerprint = new Fingerprint([]);
        var published = _folder.File("the app", "installed", "app");
        _folder.File(fingerprint.Of(_folder.PathTo("App")), "installed", PublishStep.Mark);

        var kept = await new PublishStep(_folder.PathTo("App"), _folder.PathTo("installed"), fingerprint).RunAsync(TestContext.Current.CancellationToken);

        Assert.True(kept.IsSuccess);
        Assert.Contains("unchanged", kept.Value, StringComparison.Ordinal);
        Assert.Equal("the app", File.ReadAllText(published));
        Assert.False(Directory.Exists(_folder.PathTo("installed.new")));
    }
}
