using Overworld.Redstone;

namespace Overworld.Allay.Tests.Fakes;

public sealed class Daemon : IRedstoneClient
{
    public List<string?> Signals { get; } = [];

    public Task<Response> SendAsync(Request request, CancellationToken token)
    {
        Signals.Add(request.Input?["signal"]?.GetValue<string>());

        return Task.FromResult(Response.Success(null));
    }
}
