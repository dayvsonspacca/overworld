using Overworld.Allay.Briefs;
using Overworld.Allay.Conversations;
using Overworld.Allay.Jobs;
using Overworld.Allay.Settings;

namespace Overworld.Allay.Agents;

public interface ISession
{
    string? Id { get; }

    IReadOnlyList<Job> Jobs { get; }

    event EventHandler<IReadOnlyList<Job>>? JobsChanged;

    event EventHandler<IAsyncEnumerable<Moment>>? Woke;

    IAsyncEnumerable<Moment> ReplyAsync(Prompt prompt, Setup setup, CancellationToken cancellationToken);

    void Rebrief(Brief brief);

    void Decide(string request, Decision decision);

    string? Output(string job);

    void Stop(string job);

    void End();
}
