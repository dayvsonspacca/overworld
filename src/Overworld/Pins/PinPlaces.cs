using Overworld.Bedrock.Results;

namespace Overworld.Pins;

internal sealed class PinPlaces(IReadOnlyDictionary<string, PinStore> places)
{
    public Result<PinStore> Find(string place) =>
        places.TryGetValue(place.Trim(), out var store)
            ? store
            : new Error("pins.unknown_place", $"There is no place '{place}' for pins; it is {string.Join(" or ", places.Keys)}.");
}
