using Overworld.Apps.Dye.Desktop;
using Overworld.Apps.Dye.Palettes;
using Overworld.Apps.Dye.Presentation;
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
var beacon = new BeaconClient(new RedstoneClient(SocketPath.Resolve()));

var save = Operation.From(new SavePaletteOp(store));
var list = Operation.From(new ListPalettesOp(store));
var listThemes = Operation.From(new ListThemesOp(themes));
var applyTheme = Operation.From(new ApplyThemeOp(themes, beacon));

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
    ],
    args,
    withoutArgs: () => Task.FromResult(DesktopHost.Run(app, frame =>
    {
        var dye = new DyeViewModel(new PalettesViewModel(list, save), new ThemesViewModel(listThemes, applyTheme));
        _ = dye.LoadAsync(CancellationToken.None);
        _ = FollowAsync(dye.Themes);

        var view = new DyeView { DataContext = dye };
        DyeView.Dress(frame, dye);

        return view;
    }, args)));

static async Task FollowAsync(ThemesViewModel themes)
{
    var beacon = new BeaconWatcher(SocketPath.Resolve(BeaconServer.SocketName));

    await foreach (var signal in beacon.WatchAsync(CancellationToken.None))
    {
        if (signal is "themes" or "theme")
        {
            await themes.LoadAsync(CancellationToken.None);
        }
    }
}
