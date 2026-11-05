using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Daemon.Pins;

[Operation("pins-list", "Lists the apps pinned to the menu or the taskbar, in their order.")]
internal sealed class ListPinsOp(PinPlaces places) : IOperation<ListPins, IReadOnlyList<string>>
{
    public async Task<Result<IReadOnlyList<string>>> ExecuteAsync(ListPins input, CancellationToken cancellationToken)
    {
        var store = places.Find(input.Place);
        if (!store.IsSuccess)
            return store.Error;

        return await store.Value.ListAsync(cancellationToken);
    }
}
