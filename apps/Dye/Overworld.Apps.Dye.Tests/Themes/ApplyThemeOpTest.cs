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

    private sealed class Kitty(Error? failure = null) : IKitty
    {
        public int Reloads { get; private set; }

        public Task<Result<string>> ReloadAsync(CancellationToken cancellationToken)
        {
            Reloads++;

            return Task.FromResult(failure is null ? Result.Success("kitty reloaded") : Result.Failure<string>(failure));
        }
    }

    private readonly TemporaryFolder _root = new();

    private readonly Kitty _kitty = new();

    private readonly Scheme _scheme = new();

    private readonly ThemeStore _store;

    private readonly GtkSheet _gtk;

    private readonly KittyConf _conf;

    public ApplyThemeOpTest()
    {
        var themes = System.IO.Path.Combine(_root.Path, "themes");
        Directory.CreateDirectory(System.IO.Path.Combine(themes, "amethyst"));
        File.WriteAllText(System.IO.Path.Combine(themes, "amethyst", "dark.json"), """{ "colors": { "background": "#1C1B21", "raised": "#25242B", "surface": "#2E2D35", "divider": "#38373E", "text": "#E4E2DC", "accent": "#8E62D1", "accentText": "#AB83E1", "onAccent": "#FFFFFF", "danger": "#E5806F", "added": "#8CC08C", "selected": "#332944", "border": "#4A4952", "subtle": "#8A8888", "black": "#2E2D35", "red": "#E5806F", "green": "#8CC08C", "yellow": "#E2C07A", "blue": "#7F9CE6", "magenta": "#AB83E1", "cyan": "#6FC3C4", "white": "#B7B5B2", "brightBlack": "#615F63", "brightRed": "#EE9A8B", "brightGreen": "#A6D4A3", "brightYellow": "#EDD49A", "brightBlue": "#9DB4F0", "brightMagenta": "#C9A5F2", "brightCyan": "#8FD6D5", "brightWhite": "#E4E2DC" } }""");
        File.WriteAllText(System.IO.Path.Combine(themes, "amethyst", "light.json"), """{ "colors": { "background": "#F3F2F6", "raised": "#FAF9FC", "surface": "#FFFFFF", "divider": "#E4E2E9", "text": "#1D1B22", "accent": "#7B52C4", "accentText": "#6A44B0", "onAccent": "#FFFFFF", "danger": "#C4513F", "added": "#3F8A48", "selected": "#E9E0F7", "border": "#D3D1DA", "subtle": "#75727C", "black": "#1D1B22", "red": "#C4513F", "green": "#3F8A48", "yellow": "#A0720F", "blue": "#3E5FC2", "magenta": "#7B52C4", "cyan": "#1F8586", "white": "#A8A5AF", "brightBlack": "#75727C", "brightRed": "#D6634F", "brightGreen": "#4E9E57", "brightYellow": "#B8861E", "brightBlue": "#5373D6", "brightMagenta": "#9168D6", "brightCyan": "#2E9C9C", "brightWhite": "#D9D7DE" } }""");
        _store = new ThemeStore(themes, System.IO.Path.Combine(_root.Path, "theme.json"));
        _gtk = new GtkSheet(System.IO.Path.Combine(_root.Path, "gtk-4.0", "gtk.css"));
        _conf = new KittyConf(System.IO.Path.Combine(_root.Path, "kitty", "overworld.conf"));
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _root.Dispose();

    [Fact]
    public async Task SignalsThemeOnceItIsApplied()
    {
        var daemon = new Daemon(Response.Success(null));

        var applied = await new ApplyThemeOp(_store, _gtk, _scheme, _conf, _kitty, new BeaconClient(daemon)).ExecuteAsync(new ApplyTheme(" amethyst "), Token);

        Assert.True(applied.Value.Active);
        var request = Assert.Single(daemon.Requests);
        Assert.Equal("beacon-light", request.Operation);
        Assert.Equal("theme", request.Input!["signal"]!.GetValue<string>());
    }

    [Fact]
    public async Task AppliesTheThemeEvenWithoutTheDaemon()
    {
        var daemon = new Daemon(Response.Failure(new Error("daemon.not_running", "Nothing answers.")));

        var applied = await new ApplyThemeOp(_store, _gtk, _scheme, _conf, _kitty, new BeaconClient(daemon)).ExecuteAsync(new ApplyTheme("amethyst"), Token);

        Assert.True(applied.Value.Active);
    }

    [Fact]
    public async Task SignalsNothingWhenTheThemeIsNotApplied()
    {
        var daemon = new Daemon(Response.Success(null));

        var applied = await new ApplyThemeOp(_store, _gtk, _scheme, _conf, _kitty, new BeaconClient(daemon)).ExecuteAsync(new ApplyTheme("coral"), Token);

        Assert.Equal("theme.not_found", applied.Error.Code);
        Assert.Empty(daemon.Requests);
    }

    [Fact]
    public async Task DressesGtkAppsInTheAppliedTheme()
    {
        var daemon = new Daemon(Response.Success(null));

        await new ApplyThemeOp(_store, _gtk, _scheme, _conf, _kitty, new BeaconClient(daemon)).ExecuteAsync(new ApplyTheme("amethyst"), Token);

        var sheet = await File.ReadAllTextAsync(System.IO.Path.Combine(_root.Path, "gtk-4.0", "gtk.css"), Token);
        Assert.Contains("--accent-bg-color: #8E62D1;", sheet);
    }

    [Fact]
    public async Task DressesGtkAppsInTheVariantApplied()
    {
        var daemon = new Daemon(Response.Success(null));

        var applied = await new ApplyThemeOp(_store, _gtk, _scheme, _conf, _kitty, new BeaconClient(daemon)).ExecuteAsync(new ApplyTheme("amethyst", Variant.Light), Token);

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

        var applied = await new ApplyThemeOp(_store, _gtk, scheme, _conf, _kitty, new BeaconClient(daemon)).ExecuteAsync(new ApplyTheme("amethyst"), Token);

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

        var applied = await new ApplyThemeOp(_store, _gtk, _scheme, _conf, _kitty, new BeaconClient(daemon)).ExecuteAsync(new ApplyTheme("amethyst"), Token);

        Assert.Equal("gtk.taken", applied.Error.Code);
        Assert.StartsWith("amethyst is applied", applied.Error.Message);
        Assert.Single(daemon.Requests);
        Assert.Equal("window { color: red; }", File.ReadAllText(sheet));
    }

    [Fact]
    public async Task DressesKittyInTheAppliedThemeAndReloadsIt()
    {
        var daemon = new Daemon(Response.Success(null));

        await new ApplyThemeOp(_store, _gtk, _scheme, _conf, _kitty, new BeaconClient(daemon)).ExecuteAsync(new ApplyTheme("amethyst", Variant.Light), Token);

        var conf = await File.ReadAllTextAsync(System.IO.Path.Combine(_root.Path, "kitty", "overworld.conf"), Token);
        Assert.Contains("background #F3F2F6", conf);
        Assert.Contains("color5 #7B52C4", conf);
        Assert.Equal(1, _kitty.Reloads);
    }

    [Fact]
    public async Task AppliesTheThemeButSaysWhenKittyCannotReload()
    {
        var daemon = new Daemon(Response.Success(null));
        var kitty = new Kitty(new Error("kitty.no_pkill", "pkill is not installed."));

        var applied = await new ApplyThemeOp(_store, _gtk, _scheme, _conf, kitty, new BeaconClient(daemon)).ExecuteAsync(new ApplyTheme("amethyst"), Token);

        Assert.Equal("kitty.no_pkill", applied.Error.Code);
        Assert.StartsWith("amethyst is applied, but not to kitty", applied.Error.Message);
        Assert.Single(daemon.Requests);
    }

    [Fact]
    public async Task LeavesKittyAloneWhenItsConfIsNotOverworlds()
    {
        var daemon = new Daemon(Response.Success(null));
        var conf = System.IO.Path.Combine(_root.Path, "kitty", "overworld.conf");
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(conf)!);
        File.WriteAllText(conf, "background #000000");

        var applied = await new ApplyThemeOp(_store, _gtk, _scheme, _conf, _kitty, new BeaconClient(daemon)).ExecuteAsync(new ApplyTheme("amethyst"), Token);

        Assert.Equal("kitty.taken", applied.Error.Code);
        Assert.Equal(0, _kitty.Reloads);
        Assert.Equal("background #000000", File.ReadAllText(conf));
    }
}
