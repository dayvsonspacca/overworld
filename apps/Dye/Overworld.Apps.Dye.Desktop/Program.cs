using Overworld.Allay.Bells;
using Overworld.Allay.ClaudeCode;
using Overworld.Allay.Hiring;
using Overworld.Allay.Presentation.Chats;
using Overworld.Allay.Presentation.Conversations;
using Overworld.Allay.Remote;
using Overworld.Allay.Visits;
using Overworld.Apps.Dye.Agents;
using Overworld.Apps.Dye.Palettes;
using Overworld.Apps.Dye.Profiles;
using Overworld.Apps.Dye.Presentation;
using Overworld.Apps.Dye.Presentation.Lending;
using Overworld.Apps.Dye.Presentation.Profiles;
using Overworld.Apps.Dye.Presentation.Themes;
using Overworld.Apps.Dye.Themes;
using Overworld.Beacon;
using Overworld.Bell;
using Overworld.Bedrock.Apps;
using Overworld.Bedrock.Operations;
using Overworld.Chest;
using Overworld.Hosting;
using Overworld.Redstone;
using Overworld.Web;

var app = new AppIdentity("dye");
var store = new PaletteStore(DataDirectory.For(app.Name));
var config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "overworld");
var themes = new ThemeStore(Path.Combine(config, "themes"), Path.Combine(config, "theme.json"));
var gtk = new GtkSheet(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "gtk-4.0", "gtk.css"));
var kitty = new KittyConf(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "kitty", "overworld.conf"));
var profile = new ProfileStore(Environment.UserName, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".face"));
var beacon = new BeaconClient(new RedstoneClient(SocketPath.Resolve()));

var save = Operation.From(new SavePaletteOp(store));
var list = Operation.From(new ListPalettesOp(store));
var listThemes = Operation.From(new ListThemesOp(themes));
var applyTheme = Operation.From(new ApplyThemeOp(themes, gtk, new GsettingsColorScheme(), kitty, new SignaledKitty(), beacon));
var showProfile = Operation.From(new ShowProfileOp(profile));
var setPicture = Operation.From(new SetPictureOp(profile, beacon));
var removePicture = Operation.From(new RemovePictureOp(profile, beacon));

return await AppHost.RunAsync(
    app.Command,
    [
        save,
        list,
        Operation.From(new ShowPaletteOp(store)),
        listThemes,
        Operation.From(new ShowThemeOp(themes)),
        Operation.From(new SaveThemeOp(themes, beacon)),
        applyTheme,
        showProfile,
        setPicture,
        removePicture,
    ],
    args,
    withoutArgs: () => Task.FromResult(WebHost.Run(app, page =>
    {
        var allay = DataDirectory.For("allay");
        var crew = new Crew([new ClaudeCode(Path.Combine(allay, "claude-code", "skills"))], new StaffFile(allay));
        var link = new AllayLink();
        var folder = Directory.CreateDirectory(DataDirectory.For(app.Name)).FullName;
        var brief = DyeBrief.Of(folder, Environment.ProcessPath ?? app.Command);
        var ringer = new Ringer(new BellClient(new RedstoneClient(SocketPath.Resolve())), app.Name);
        var chat = new ChatViewModel(new Lobby(crew, link), _ => Task.FromResult(brief), [new DyeLender()], DyeBrief.Suggestions, ringer, "Dye");
        page.Activated += (_, _) => chat.IsAttended = true;
        page.Deactivated += (_, _) => chat.IsAttended = false;
        chat.Summoned += (_, _) => page.Summon();
        _ = ringer.FollowAsync(new BeaconWatcher(SocketPath.Resolve(BeaconServer.SocketName)).WatchAsync(CancellationToken.None), CancellationToken.None);
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            chat.End();
            link.Dispose();
        };
        var dye = new DyeViewModel(
            new PalettesViewModel(list, save),
            new ThemesViewModel(listThemes, applyTheme),
            new ProfileViewModel(showProfile, setPicture, removePicture),
            chat);
        _ = dye.LoadAsync(CancellationToken.None);
        _ = FollowAsync(dye);

        page.Mirror.Ignore<ThemeLook>(nameof(ThemeLook.Theme));

        foreach (var (type, property) in Hidden.Properties)
        {
            page.Mirror.Ignore(type, property);
        }

        page.Handle("picture", async picked =>
        {
            if (picked?["data"]?.GetValue<string>() is { Length: > 0 } data)
            {
                var file = Path.Combine(Path.GetTempPath(), $"overworld-picture{Path.GetExtension(picked["name"]?.GetValue<string>() ?? ".png")}");
                await File.WriteAllBytesAsync(file, Convert.FromBase64String(data));
                await dye.Profile.SetPictureAsync(file, CancellationToken.None);
            }

            return null;
        });
        page.Show(dye);
    })));

static async Task FollowAsync(DyeViewModel dye)
{
    var beacon = new BeaconWatcher(SocketPath.Resolve(BeaconServer.SocketName));

    await foreach (var signal in beacon.WatchAsync(CancellationToken.None))
    {
        if (signal is "themes" or "theme")
        {
            await dye.Themes.LoadAsync(CancellationToken.None);
        }
        else if (signal is "profile")
        {
            await dye.Profile.LoadAsync(CancellationToken.None);
        }
    }
}
