using Overworld.Bedrock.Results;
using Overworld.Chest;

namespace Overworld.Pins;

internal sealed class PinStore(string path)
{
    private readonly JsonFile<List<string>> _file = new(path, () => []);

    public async Task<Result<IReadOnlyList<string>>> ListAsync(CancellationToken token) =>
        Listed(await _file.ReadAsync(token));

    public async Task<Result<IReadOnlyList<string>>> AddAsync(string app, CancellationToken token) =>
        Listed(await _file.UpdateAsync(pins => pins.Contains(app) ? pins : [.. pins, app], token));

    public async Task<Result<IReadOnlyList<string>>> RemoveAsync(string app, CancellationToken token) =>
        Listed(await _file.UpdateAsync(pins => pins.Contains(app) ? pins.Where(pin => pin != app).ToList() : pins, token));

    private static Result<IReadOnlyList<string>> Listed(Result<List<string>> pins) =>
        pins.IsSuccess ? pins.Value : pins.Error;
}
