using Overworld.Apps.Painting.Artworks;
using Overworld.Apps.Painting.Presentation;
using Overworld.Beacon;
using Overworld.Bedrock.Apps;
using Overworld.Bedrock.Operations;
using Overworld.Chest;
using Overworld.Hosting;
using Overworld.Redstone;
using Overworld.Web;

var app = new AppIdentity("painting");
var store = new ArtworkStore(DataDirectory.For(app.Name));
var beacon = new BeaconClient(new RedstoneClient(SocketPath.Resolve()));

var show = Operation.From(new ShowArtworkOp(store));

return await AppHost.RunAsync(
    app.Command,
    [Operation.From(new PaintArtworkOp(store, beacon, TimeProvider.System)), show],
    args,
    withoutArgs: () => Task.FromResult(WebHost.Run(app, page =>
    {
        var painting = new PaintingViewModel(show);
        _ = painting.LoadAsync(CancellationToken.None);
        _ = FollowAsync(painting);
        page.Show(painting);
    })));

static async Task FollowAsync(PaintingViewModel painting)
{
    var beacon = new BeaconWatcher(SocketPath.Resolve(BeaconServer.SocketName));

    await foreach (var signal in beacon.WatchAsync(CancellationToken.None))
    {
        if (signal is "painting")
        {
            await painting.LoadAsync(CancellationToken.None);
        }
    }
}
