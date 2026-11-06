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

    private readonly TemporaryFolder _root = new();

    private readonly ThemeStore _store;

    public ApplyThemeOpTest()
    {
        var themes = System.IO.Path.Combine(_root.Path, "themes");
        Directory.CreateDirectory(themes);
        File.WriteAllText(System.IO.Path.Combine(themes, "amethyst.json"), """{ "colors": { "accent": "#8E62D1" } }""");
        _store = new ThemeStore(themes, System.IO.Path.Combine(_root.Path, "theme.json"));
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _root.Dispose();

    [Fact]
    public async Task SignalsThemeOnceItIsApplied()
    {
        var daemon = new Daemon(Response.Success(null));

        var applied = await new ApplyThemeOp(_store, new BeaconClient(daemon)).ExecuteAsync(new ApplyTheme(" amethyst "), Token);

        Assert.True(applied.Value.Active);
        var request = Assert.Single(daemon.Requests);
        Assert.Equal("beacon-light", request.Operation);
        Assert.Equal("theme", request.Input!["signal"]!.GetValue<string>());
    }

    [Fact]
    public async Task AppliesTheThemeEvenWithoutTheDaemon()
    {
        var daemon = new Daemon(Response.Failure(new Error("daemon.not_running", "Nothing answers.")));

        var applied = await new ApplyThemeOp(_store, new BeaconClient(daemon)).ExecuteAsync(new ApplyTheme("amethyst"), Token);

        Assert.True(applied.Value.Active);
    }

    [Fact]
    public async Task SignalsNothingWhenTheThemeIsNotApplied()
    {
        var daemon = new Daemon(Response.Success(null));

        var applied = await new ApplyThemeOp(_store, new BeaconClient(daemon)).ExecuteAsync(new ApplyTheme("coral"), Token);

        Assert.Equal("theme.not_found", applied.Error.Code);
        Assert.Empty(daemon.Requests);
    }
}
