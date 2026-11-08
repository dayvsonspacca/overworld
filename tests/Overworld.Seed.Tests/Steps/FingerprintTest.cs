using Overworld.Seed.Steps;

namespace Overworld.Seed.Tests.Steps;

public sealed class FingerprintTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();

    private readonly Fingerprint _fingerprint;

    private readonly string _app;

    public FingerprintTest()
    {
        _folder.File("""<Project><ItemGroup><ProjectReference Include="../Core/Core.csproj" /></ItemGroup></Project>""", "App", "App.csproj");
        _folder.File("class Program;", "App", "Program.cs");
        _folder.File("<Project />", "Core", "Core.csproj");
        _folder.File("class Shared;", "Core", "Shared.cs");
        _folder.File("<Project />", "Other", "Other.csproj");
        _folder.File("class Other;", "Other", "Other.cs");
        _fingerprint = new Fingerprint([_folder.File("<Project />", "Directory.Build.props")]);
        _app = _folder.PathTo("App");
    }

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void StaysTheSameWhileNothingChanges()
    {
        Assert.Equal(_fingerprint.Of(_app), _fingerprint.Of(_app));
    }

    [Theory]
    [InlineData("App", "Program.cs")]
    [InlineData("Core", "Shared.cs")]
    [InlineData("Directory.Build.props")]
    public void ChangesWithWhatGoesIntoTheProject(params string[] file)
    {
        var before = _fingerprint.Of(_app);

        _folder.File("changed", file);

        Assert.NotEqual(before, _fingerprint.Of(_app));
    }

    [Fact]
    public void ChangesWhenAFileIsAdded()
    {
        var before = _fingerprint.Of(_app);

        _folder.File("class Added;", "Core", "Added.cs");

        Assert.NotEqual(before, _fingerprint.Of(_app));
    }

    [Theory]
    [InlineData("App", "bin", "App.dll")]
    [InlineData("Core", "obj", "project.assets.json")]
    [InlineData("Other", "Other.cs")]
    public void StaysTheSameWithWhatDoesNotGoIntoTheProject(params string[] file)
    {
        var before = _fingerprint.Of(_app);

        _folder.File("changed", file);

        Assert.Equal(before, _fingerprint.Of(_app));
    }
}
