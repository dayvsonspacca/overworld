using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Pins;

[Operation("pins-remove", "Unpins an app from the menu or the taskbar, and lists that place's pins.")]
internal sealed class RemovePinOp(PinPlaces places) : IOperation<RemovePin, IReadOnlyList<string>>
{
    public async Task<Result<IReadOnlyList<string>>> ExecuteAsync(RemovePin input, CancellationToken cancellationToken)
    {
        var app = input.App.Trim();
        if (app.Length == 0)
            return new Error("pins.app_empty", "A pin needs an app id, like firefox.");

        var store = places.Find(input.Place);
        if (!store.IsSuccess)
            return store.Error;

        return await store.Value.RemoveAsync(app, cancellationToken);
    }
}
