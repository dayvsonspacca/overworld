using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Projects;

namespace Overworld.Apps.Crafter.Agents;

public interface IKeepsHistory
{
    Task<Recollection> History(Project project, int latest, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Turn>> Replay(Project project, string sessionId, CancellationToken cancellationToken = default);
}
