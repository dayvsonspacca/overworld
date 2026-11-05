namespace Overworld.Redstone;

public interface IRedstoneClient
{
    Task<Response> SendAsync(Request request, CancellationToken token);
}
