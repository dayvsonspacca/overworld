using Overworld.Allay.Bells;
using Overworld.Allay.ClaudeCode;
using Overworld.Allay.Hiring;
using Overworld.Allay.Presentation.Chats;
using Overworld.Allay.Presentation.Conversations;
using Overworld.Allay.Remote;
using Overworld.Allay.Visits;
using Overworld.Apps.Painting.Agents;
using Overworld.Apps.Painting.Artworks;
using Overworld.Apps.Painting.Presentation;
using Overworld.Apps.Painting.Presentation.Lending;
using Overworld.Beacon;
using Overworld.Bell;
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
var list = Operation.From(new ListArtworksOp(store));

return await AppHost.RunAsync(
    app.Command,
    [Operation.From(new PaintArtworkOp(store, beacon, TimeProvider.System)), show, list, Operation.From(new RestoreArtworkOp(store, beacon, TimeProvider.System)), Operation.From(new GroupArtworkOp(store, beacon))],
    args,
    withoutArgs: () => Task.FromResult(WebHost.Run(app, page =>
    {
        var allay = DataDirectory.For("allay");
        var crew = new Crew([new ClaudeCode(Path.Combine(allay, "claude-code", "skills"))], new StaffFile(allay));
        var link = new AllayLink();
        var folder = Directory.CreateDirectory(DataDirectory.For(app.Name)).FullName;
        var brief = PaintingBrief.Of(folder, Environment.ProcessPath ?? app.Command);
        var ringer = new Ringer(new BellClient(new RedstoneClient(SocketPath.Resolve())), app.Name);
        var lender = new PaintingLender(show);
        var chat = new ChatViewModel(new Lobby(crew, link), _ => Task.FromResult(brief), [lender], PaintingBrief.Suggestions, ringer, "Painting");
        _ = lender.FollowAsync(new BeaconWatcher(SocketPath.Resolve(BeaconServer.SocketName)).WatchAsync(CancellationToken.None), CancellationToken.None);
        page.Activated += (_, _) => chat.IsAttended = true;
        page.Deactivated += (_, _) => chat.IsAttended = false;
        chat.Summoned += (_, _) => page.Summon();
        _ = ringer.FollowAsync(new BeaconWatcher(SocketPath.Resolve(BeaconServer.SocketName)).WatchAsync(CancellationToken.None), CancellationToken.None);
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            chat.End();
            link.Dispose();
        };

        foreach (var (type, property) in Hidden.Properties)
        {
            page.Mirror.Ignore(type, property);
        }

        var painting = new PaintingViewModel(list, show, chat);
        _ = chat.LoadAsync(CancellationToken.None);
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
