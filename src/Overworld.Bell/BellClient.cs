using System.Text.Json;
using Overworld.Bedrock.Json;
using Overworld.Bedrock.Results;
using Overworld.Redstone;

namespace Overworld.Bell;

public sealed class BellClient(IRedstoneClient client)
{
    public Task<Result<Notice>> RingAsync(RingBell ring, CancellationToken token) =>
        SendAsync<RingBell, Notice>(RingBellOp.Name, ring, token);

    public Task<Result<Notice>> WithdrawAsync(string id, CancellationToken token) =>
        SendAsync<WithdrawNotice, Notice>(WithdrawNoticeOp.Name, new WithdrawNotice(id), token);

    public Task<Result<IReadOnlyList<Notice>>> CollectAsync(string from, CancellationToken token) =>
        SendAsync<CollectNotices, IReadOnlyList<Notice>>(CollectNoticesOp.Name, new CollectNotices(from), token);

    private async Task<Result<TOutput>> SendAsync<TInput, TOutput>(string operation, TInput input, CancellationToken token)
    {
        var request = new Request(operation, JsonSerializer.SerializeToNode(input, BedrockJson.Options));
        var response = await client.SendAsync(request, token);

        if (response.Error is { } error)
        {
            return error;
        }

        return response.Result.Deserialize<TOutput>(BedrockJson.Options) is { } output
            ? output
            : new Error("bell.no_result", $"The daemon answered {operation} with nothing.");
    }
}
