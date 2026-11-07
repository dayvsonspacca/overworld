using Overworld.Apps.Dye.Desktop;
using Overworld.Apps.Dye.Palettes;
using Overworld.Apps.Dye.Profiles;
using Overworld.Apps.Dye.Presentation;
using Overworld.Apps.Dye.Presentation.Profiles;
using Overworld.Apps.Dye.Presentation.Themes;
using Overworld.Apps.Dye.Themes;
using Overworld.Beacon;
using Overworld.Bedrock.Apps;
using Overworld.Bedrock.Operations;
using Overworld.Chest;
using Overworld.Desktop;
using Overworld.Hosting;
using Overworld.Redstone;

var app = new AppIdentity("dye");
var store = new PaletteStore(DataDirectory.For(app.Name));
var config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "overworld");
var themes = new ThemeStore(Path.Combine(config, "themes"), Path.Combine(config, "theme.json"));
var profile = new ProfileStore(Environment.UserName, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".face"));
var beacon = new BeaconClient(new RedstoneClient(SocketPath.Resolve()));

var save = Operation.From(new SavePaletteOp(store));
var list = Operation.From(new ListPalettesOp(store));
var listThemes = Operation.From(new ListThemesOp(themes));
var applyTheme = Operation.From(new ApplyThemeOp(themes, beacon));
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
    withoutArgs: () => Task.FromResult(DesktopHost.Run(app, frame =>
    {
        var dye = new DyeViewModel(
            new PalettesViewModel(list, save),
            new ThemesViewModel(listThemes, applyTheme),
            new ProfileViewModel(showProfile, setPicture, removePicture));
        _ = dye.LoadAsync(CancellationToken.None);
        _ = FollowAsync(dye);

        var view = new DyeView { DataContext = dye };
        DyeView.Dress(frame, dye);

        return view;
    }, args)));

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
