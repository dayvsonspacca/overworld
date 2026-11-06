using Overworld.Allay.Conversations;

namespace Overworld.Allay.Agents;

public interface IKeepsHistory
{
    Task<Recollection> ListAsync(string folder, int latest, CancellationToken cancellationToken);

    Task<IReadOnlyList<Turn>> ReplayAsync(string folder, string session, CancellationToken cancellationToken);
}
