using Overworld.Apps.Dye.Desktop;
using Overworld.Apps.Dye.Palettes;
using Overworld.Apps.Dye.Presentation;
using Overworld.Bedrock.Apps;
using Overworld.Bedrock.Hosting;
using Overworld.Bedrock.Operations;

var app = new AppIdentity("dye");
var store = new PaletteStore(app.DataDirectory);

var list = Operation.From(new ListPalettesOp(store));

return await AppHost.RunAsync(
    app,
    [
        Operation.From(new SavePaletteOp(store)),
        list,
        Operation.From(new ShowPaletteOp(store)),
    ],
    args,
    window: () =>
    {
        var palettes = new PalettesViewModel(list);
        _ = palettes.LoadAsync(CancellationToken.None);

        return new PalettesView { DataContext = palettes };
    });
