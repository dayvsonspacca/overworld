using Overworld.Beacon;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Daemon.Pins;

[Operation("pins-add", "Pins an app to the end of the menu or the taskbar, and lists that place's pins.")]
internal sealed class AddPinOp(PinPlaces places, Beam beam) : IOperation<AddPin, IReadOnlyList<string>>
{
    public async Task<Result<IReadOnlyList<string>>> ExecuteAsync(AddPin input, CancellationToken cancellationToken)
    {
        var app = input.App.Trim();
        if (app.Length == 0)
            return new Error("pins.app_empty", "A pin needs an app id, like firefox.");

        var store = places.Find(input.Place);
        if (!store.IsSuccess)
            return store.Error;

        var pins = await store.Value.AddAsync(app, cancellationToken);
        if (pins.IsSuccess)
            beam.Light("pins");

        return pins;
    }
}
