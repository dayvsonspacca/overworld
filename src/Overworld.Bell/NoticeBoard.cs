using Overworld.Bedrock.Results;
using Overworld.Chest;

namespace Overworld.Bell;

public sealed class NoticeBoard(string path)
{
    private readonly JsonFile<List<Notice>> _file = new(path, () => [], notices => notices.Count == 0);

    public async Task<Result<IReadOnlyList<Notice>>> ListAsync(CancellationToken token)
    {
        var notices = await _file.ReadAsync(token);

        return notices.IsSuccess ? notices.Value.Where(notice => notice.IsWaiting).ToList() : notices.Error;
    }

    public async Task<Result<Notice>> RingAsync(Notice notice, CancellationToken token)
    {
        var notices = await _file.UpdateAsync(notices => notices.Append(notice).ToList(), token);

        return notices.IsSuccess ? notice : notices.Error;
    }

    public Task<Result<Notice>> AnswerAsync(string id, Reply reply, CancellationToken token) =>
        ChangeAsync(id, notice =>
        {
            if (!notice.IsWaiting)
            {
                return new Error("bell.answered", $"Notice {id} is answered already.");
            }

            return Fits(notice.Ask, reply)
                ? notice with { Reply = reply }
                : new Error("bell.reply_unfit", $"That reply doesn't answer what notice {id} asks.");
        }, token);

    public Task<Result<Notice>> OpenAsync(string id, CancellationToken token) =>
        ChangeAsync(id, notice => notice with { Opened = true }, token);

    public async Task<Result<Notice>> WithdrawAsync(string id, CancellationToken token)
    {
        Notice? withdrawn = null;

        var notices = await _file.UpdateAsync(notices =>
        {
            withdrawn = notices.Find(notice => notice.Id == id);

            return withdrawn is null ? Unknown(id) : notices.Where(notice => notice.Id != id).ToList();
        }, token);

        return notices.IsSuccess ? withdrawn! : notices.Error;
    }

    public async Task<Result<IReadOnlyList<Notice>>> CollectAsync(string from, CancellationToken token)
    {
        List<Notice> collected = [];

        var notices = await _file.UpdateAsync(notices =>
        {
            collected = notices.Where(notice => notice.From == from && (!notice.IsWaiting || notice.Opened)).ToList();

            if (collected.Count == 0)
            {
                return notices;
            }

            return notices
                .Where(notice => notice.From != from || notice.IsWaiting)
                .Select(notice => notice.From == from ? notice with { Opened = false } : notice)
                .ToList();
        }, token);

        return notices.IsSuccess ? collected : notices.Error;
    }

    private async Task<Result<Notice>> ChangeAsync(string id, Func<Notice, Result<Notice>> change, CancellationToken token)
    {
        Notice? changed = null;

        var notices = await _file.UpdateAsync(notices =>
        {
            var index = notices.FindIndex(notice => notice.Id == id);

            if (index < 0)
            {
                return Unknown(id);
            }

            var result = change(notices[index]);

            if (!result.IsSuccess)
            {
                return result.Error;
            }

            changed = result.Value;

            return notices.Select((notice, at) => at == index ? changed : notice).ToList();
        }, token);

        return notices.IsSuccess ? changed! : notices.Error;
    }

    private static bool Fits(Ask? ask, Reply reply) => (ask, reply) switch
    {
        (Questions questions, Answered answered) => answered.Given.Count == questions.Asked.Count,
        (Questions, Skipped) => true,
        (Buttons buttons, Pressed pressed) => buttons.Offered.Any(button => button.Key == pressed.Key),
        _ => false,
    };

    private static Error Unknown(string id) =>
        new("bell.unknown", $"There is no notice {id}; it was withdrawn, collected or never rung.");
}
