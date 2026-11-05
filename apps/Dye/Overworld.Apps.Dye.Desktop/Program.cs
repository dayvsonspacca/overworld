using Overworld.Apps.Dye.Palettes;
using Overworld.Bedrock.Apps;
using Overworld.Bedrock.Hosting;
using Overworld.Bedrock.Operations;

var app = new AppIdentity("dye");
var store = new PaletteStore(app.DataDirectory);

return await AppHost.RunAsync(
    app,
    [
        Operation.From(new SavePaletteOp(store)),
        Operation.From(new ListPalettesOp(store)),
        Operation.From(new ShowPaletteOp(store)),
    ],
    args);
