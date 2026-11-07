using Overworld.Apps.Dye.Themes;
using Overworld.Beacon;
using Overworld.Bedrock.Results;
using Overworld.Redstone;

namespace Overworld.Apps.Dye.Tests.Themes;

public sealed class ApplyThemeOpTest : IDisposable
{
    private sealed class Daemon(Response answer) : IRedstoneClient
    {
        public List<Request> Requests { get; } = [];

        public Task<Response> SendAsync(Request request, CancellationToken token)
        {
            Requests.Add(request);

            return Task.FromResult(answer);
        }
    }

    private sealed class Scheme(Error? failure = null) : IColorScheme
    {
        public List<Variant> Preferred { get; } = [];

        public Task<Result<string>> PreferAsync(Variant variant, CancellationToken cancellationToken)
        {
            Preferred.Add(variant);

            return Task.FromResult(failure is null ? Result.Success(variant.ToString()) : Result.Failure<string>(failure));
        }
    }

    private readonly TemporaryFolder _root = new();

    private readonly Scheme _scheme = new();

    private readonly ThemeStore _store;

    private readonly GtkSheet _gtk;

    public ApplyThemeOpTest()
    {
        var themes = System.IO.Path.Combine(_root.Path, "themes");
        Directory.CreateDirectory(System.IO.Path.Combine(themes, "amethyst"));
        File.WriteAllText(System.IO.Path.Combine(themes, "amethyst", "dark.json"), """{ "colors": { "background": "#1C1B21", "raised": "#25242B", "surface": "#2E2D35", "divider": "#38373E", "text": "#E4E2DC", "accent": "#8E62D1", "accentText": "#AB83E1", "onAccent": "#FFFFFF", "danger": "#E5806F", "added": "#8CC08C" } }""");
        File.WriteAllText(System.IO.Path.Combine(themes, "amethyst", "light.json"), """{ "colors": { "background": "#F3F2F6", "raised": "#FAF9FC", "surface": "#FFFFFF", "divider": "#E4E2E9", "text": "#1D1B22", "accent": "#7B52C4", "accentText": "#6A44B0", "onAccent": "#FFFFFF", "danger": "#C4513F", "added": "#3F8A48" } }""");
        _store = new ThemeStore(themes, System.IO.Path.Combine(_root.Path, "theme.json"));
        _gtk = new GtkSheet(System.IO.Path.Combine(_root.Path, "gtk-4.0", "gtk.css"));
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _root.Dispose();

    [Fact]
    public async Task SignalsThemeOnceItIsApplied()
    {
        var daemon = new Daemon(Response.Success(null));

        var applied = await new ApplyThemeOp(_store, _gtk, _scheme, new BeaconClient(daemon)).ExecuteAsync(new ApplyTheme(" amethyst "), Token);

        Assert.True(applied.Value.Active);
        var request = Assert.Single(daemon.Requests);
        Assert.Equal("beacon-light", request.Operation);
        Assert.Equal("theme", request.Input!["signal"]!.GetValue<string>());
    }

    [Fact]
    public async Task AppliesTheThemeEvenWithoutTheDaemon()
    {
        var daemon = new Daemon(Response.Failure(new Error("daemon.not_running", "Nothing answers.")));

        var applied = await new ApplyThemeOp(_store, _gtk, _scheme, new BeaconClient(daemon)).ExecuteAsync(new ApplyTheme("amethyst"), Token);

        Assert.True(applied.Value.Active);
    }

    [Fact]
    public async Task SignalsNothingWhenTheThemeIsNotApplied()
    {
        var daemon = new Daemon(Response.Success(null));

        var applied = await new ApplyThemeOp(_store, _gtk, _scheme, new BeaconClient(daemon)).ExecuteAsync(new ApplyTheme("coral"), Token);

        Assert.Equal("theme.not_found", applied.Error.Code);
        Assert.Empty(daemon.Requests);
    }

    [Fact]
    public async Task DressesGtkAppsInTheAppliedTheme()
    {
        var daemon = new Daemon(Response.Success(null));

        await new ApplyThemeOp(_store, _gtk, _scheme, new BeaconClient(daemon)).ExecuteAsync(new ApplyTheme("amethyst"), Token);

        var sheet = await File.ReadAllTextAsync(System.IO.Path.Combine(_root.Path, "gtk-4.0", "gtk.css"), Token);
        Assert.Contains("--accent-bg-color: #8E62D1;", sheet);
    }

    [Fact]
    public async Task DressesGtkAppsInTheVariantApplied()
    {
        var daemon = new Daemon(Response.Success(null));

        var applied = await new ApplyThemeOp(_store, _gtk, _scheme, new BeaconClient(daemon)).ExecuteAsync(new ApplyTheme("amethyst", Variant.Light), Token);

        Assert.Equal(Variant.Light, applied.Value.Worn);
        var sheet = await File.ReadAllTextAsync(System.IO.Path.Combine(_root.Path, "gtk-4.0", "gtk.css"), Token);
        Assert.Contains("--window-bg-color: #F3F2F6;", sheet);
        Assert.Equal([Variant.Light], _scheme.Preferred);
    }

    [Fact]
    public async Task AppliesTheThemeButSaysWhenGtkAppsCannotLearnItsVariant()
    {
        var daemon = new Daemon(Response.Success(null));
        var scheme = new Scheme(new Error("gtk.no_gsettings", "gsettings is not installed."));

        var applied = await new ApplyThemeOp(_store, _gtk, scheme, new BeaconClient(daemon)).ExecuteAsync(new ApplyTheme("amethyst"), Token);

        Assert.Equal("gtk.no_gsettings", applied.Error.Code);
        Assert.StartsWith("amethyst is applied", applied.Error.Message);
        Assert.Single(daemon.Requests);
    }

    [Fact]
    public async Task AppliesTheThemeButSaysWhenGtkAppsCannotWearIt()
    {
        var daemon = new Daemon(Response.Success(null));
        var sheet = System.IO.Path.Combine(_root.Path, "gtk-4.0", "gtk.css");
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(sheet)!);
        File.WriteAllText(sheet, "window { color: red; }");

        var applied = await new ApplyThemeOp(_store, _gtk, _scheme, new BeaconClient(daemon)).ExecuteAsync(new ApplyTheme("amethyst"), Token);

        Assert.Equal("gtk.taken", applied.Error.Code);
        Assert.StartsWith("amethyst is applied", applied.Error.Message);
        Assert.Single(daemon.Requests);
        Assert.Equal("window { color: red; }", File.ReadAllText(sheet));
    }
}
