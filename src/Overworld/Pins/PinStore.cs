using Overworld.Bedrock.Chest;
using Overworld.Bedrock.Results;

namespace Overworld.Pins;

internal sealed class PinStore(string path)
{
    private readonly JsonFile<List<string>> _file = new(path, () => []);

    public Task<Result<IReadOnlyList<string>>> ListAsync(CancellationToken token) =>
        ChangeAsync(_ => false, token);

    public Task<Result<IReadOnlyList<string>>> AddAsync(string app, CancellationToken token) =>
        ChangeAsync(pins =>
        {
            if (pins.Contains(app))
                return false;

            pins.Add(app);
            return true;
        }, token);

    public Task<Result<IReadOnlyList<string>>> RemoveAsync(string app, CancellationToken token) =>
        ChangeAsync(pins => pins.Remove(app), token);

    private async Task<Result<IReadOnlyList<string>>> ChangeAsync(Func<List<string>, bool> change, CancellationToken token)
    {
        var pins = await _file.ReadAsync(token);
        if (!pins.IsSuccess)
            return pins.Error;

        if (change(pins.Value))
            await _file.WriteAsync(pins.Value, token);

        return Result.Success<IReadOnlyList<string>>(pins.Value);
    }
}
