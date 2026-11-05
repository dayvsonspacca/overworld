namespace Overworld.Redstone;

internal interface IRedstoneClient
{
    Task<Response> SendAsync(Request request, CancellationToken token);
}
