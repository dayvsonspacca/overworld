using Overworld.Apps.Dye.Profiles;
using Overworld.Beacon;
using Overworld.Redstone;

namespace Overworld.Apps.Dye.Tests.Profiles;

public sealed class ProfileOpsTest : IDisposable
{
    private sealed class Daemon : IRedstoneClient
    {
        public List<Request> Requests { get; } = [];

        public Task<Response> SendAsync(Request request, CancellationToken token)
        {
            Requests.Add(request);

            return Task.FromResult(Response.Success(null));
        }
    }

    private readonly TemporaryFolder _root = new();

    private readonly Daemon _daemon = new();

    private readonly ProfileStore _store;

    public ProfileOpsTest() => _store = new ProfileStore("steve", Path.Combine(_root.Path, ".face"));

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _root.Dispose();

    [Fact]
    public async Task ShowsTheProfile()
    {
        var shown = await new ShowProfileOp(_store).ExecuteAsync(new ShowProfile(), Token);

        Assert.Equal(new Profile("steve", null), shown.Value);
    }

    [Fact]
    public async Task SignalsTheShellWhenThePictureIsSet()
    {
        var source = Path.Combine(_root.Path, "me.png");
        File.WriteAllBytes(source, [1]);

        var set = await new SetPictureOp(_store, new BeaconClient(_daemon)).ExecuteAsync(new SetPicture($" {source} "), Token);

        Assert.NotNull(set.Value.Picture);
        var request = Assert.Single(_daemon.Requests);
        Assert.Equal("beacon-light", request.Operation);
        Assert.Equal("profile", request.Input!["signal"]!.GetValue<string>());
    }

    [Fact]
    public async Task SignalsNothingWhenThePictureIsRefused()
    {
        var set = await new SetPictureOp(_store, new BeaconClient(_daemon)).ExecuteAsync(new SetPicture("gone.png"), Token);

        Assert.False(set.IsSuccess);
        Assert.Empty(_daemon.Requests);
    }

    [Fact]
    public async Task SignalsTheShellWhenThePictureIsRemoved()
    {
        var removed = await new RemovePictureOp(_store, new BeaconClient(_daemon)).ExecuteAsync(new RemovePicture(), Token);

        Assert.Null(removed.Value.Picture);
        Assert.Single(_daemon.Requests);
    }
}
