using Overworld.Apps.Dye.Desktop;
using Overworld.Apps.Dye.Palettes;
using Overworld.Apps.Dye.Presentation;
using Overworld.Bedrock.Apps;
using Overworld.Bedrock.Desktop;
using Overworld.Bedrock.Hosting;
using Overworld.Bedrock.Operations;
using Overworld.Chest;

var app = new AppIdentity("dye");
var store = new PaletteStore(DataDirectory.For(app.Name));

var save = Operation.From(new SavePaletteOp(store));
var list = Operation.From(new ListPalettesOp(store));

return await AppHost.RunAsync(
    app.Command,
    [
        save,
        list,
        Operation.From(new ShowPaletteOp(store)),
    ],
    args,
    withoutArgs: () => Task.FromResult(DesktopHost.Run(app, () =>
    {
        var palettes = new PalettesViewModel(list, save);
        _ = palettes.LoadAsync(CancellationToken.None);

        return new PalettesView { DataContext = palettes };
    }, args)));
