using Overworld.Redstone;

namespace Overworld.Apps.Allay.Presentation.Tests.Fakes;

public sealed class Daemon : IRedstoneClient
{
    public Task<Response> SendAsync(Request request, CancellationToken token) => Task.FromResult(Response.Success(null));
}
