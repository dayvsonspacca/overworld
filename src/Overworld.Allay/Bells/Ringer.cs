using System.Collections.Concurrent;
using Overworld.Allay.Conversations;
using Overworld.Bell;

namespace Overworld.Allay.Bells;

public sealed class Ringer(BellClient bell, string from)
{
    private readonly ConcurrentDictionary<string, Rung> _rung = new();

    public async Task<string?> RingAsync(Request request, Ringing ringing, CancellationToken token)
    {
        var rung = await bell.RingAsync(RequestBell.Ring(request, from, ringing.Context, ringing.Color, ringing.Quiet), token);

        if (!rung.IsSuccess)
        {
            return null;
        }

        _rung[rung.Value.Id] = new Rung(request, ringing);

        return rung.Value.Id;
    }

    public async Task WithdrawAsync(string notice, CancellationToken token)
    {
        if (_rung.TryRemove(notice, out _))
        {
            await bell.WithdrawAsync(notice, token);
        }
    }

    public Task WithdrawAllAsync(CancellationToken token) =>
        Task.WhenAll(_rung.Keys.ToList().Select(notice => WithdrawAsync(notice, token)));

    public async Task FollowAsync(IAsyncEnumerable<string> signals, CancellationToken token)
    {
        await CollectAsync(token);

        await foreach (var signal in signals.WithCancellation(token))
        {
            if (signal == Signal.Name)
            {
                await CollectAsync(token);
            }
        }
    }

    private async Task CollectAsync(CancellationToken token)
    {
        var collected = await bell.CollectAsync(from, token);

        if (!collected.IsSuccess)
        {
            return;
        }

        foreach (var notice in collected.Value)
        {
            if (notice.Reply is { } reply && _rung.TryRemove(notice.Id, out var answered))
            {
                answered.Ringing.Decide(RequestBell.Decision(answered.Request, reply));
            }
            else if (notice.Opened && _rung.TryGetValue(notice.Id, out var opened))
            {
                opened.Ringing.Open();
            }
        }
    }

    private sealed record Rung(Request Request, Ringing Ringing);
}
