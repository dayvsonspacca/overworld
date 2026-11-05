using Overworld.Seed.Steps;

namespace Overworld.Seed.Tests.Steps;

public sealed class DesktopEntryStepTest : IDisposable
{
    private const string Template = "Exec=\"@APPS@/dye/dye\"\nIcon=@APPS@/dye/@ICON@\n";

    private readonly TemporaryFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    private string Apps => _folder.PathTo("apps");

    private string Applications => _folder.PathTo("applications");

    private Task<Bedrock.Results.Result<string>> Run() =>
        new DesktopEntryStep("dye", _folder.PathTo("repo", "Dye"), Apps, Applications)
            .RunAsync(TestContext.Current.CancellationToken);

    [Fact]
    public async Task FillsTheTemplateWithWhereTheAppIsInstalled()
    {
        _folder.Folder("apps", "dye");
        _folder.File(Template, "repo", "Dye", "dye.desktop");
        _folder.File("<svg/>", "repo", "Dye", "dye.svg");

        var added = await Run();

        Assert.True(added.IsSuccess);
        Assert.Equal(
            $"Exec=\"{Apps}/dye/dye\"\nIcon={Apps}/dye/icon.svg\n",
            await File.ReadAllTextAsync(Path.Combine(Applications, "dye.desktop"), TestContext.Current.CancellationToken));
        Assert.True(File.Exists(_folder.PathTo("apps", "dye", "icon.svg")));
    }

    [Fact]
    public async Task PrefersTheLocalIconOverTheSharedOne()
    {
        _folder.Folder("apps", "dye");
        _folder.File(Template, "repo", "Dye", "dye.desktop");
        _folder.File("<svg/>", "repo", "Dye", "dye.svg");
        _folder.File("png", "repo", "Dye", "icon.png");

        await Run();

        Assert.Contains("icon.png", await File.ReadAllTextAsync(Path.Combine(Applications, "dye.desktop"), TestContext.Current.CancellationToken));
        Assert.True(File.Exists(_folder.PathTo("apps", "dye", "icon.png")));
    }

    [Fact]
    public async Task RefusesAnAppThatIsNotPublished()
    {
        _folder.File(Template, "repo", "Dye", "dye.desktop");
        _folder.File("<svg/>", "repo", "Dye", "dye.svg");

        var added = await Run();

        Assert.Equal("seed.missing_source", added.Error.Code);
        Assert.False(Directory.Exists(Applications));
    }

    [Fact]
    public async Task RefusesAnAppWithoutAnIcon()
    {
        _folder.Folder("apps", "dye");
        _folder.File(Template, "repo", "Dye", "dye.desktop");

        var added = await Run();

        Assert.Equal("seed.missing_icon", added.Error.Code);
    }
}
