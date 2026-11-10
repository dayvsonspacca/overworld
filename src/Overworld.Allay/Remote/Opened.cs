using Overworld.Allay.Conversations;

namespace Overworld.Allay.Remote;

public sealed record Opened(RemoteSession Session, IReadOnlyList<Turn> Turns);
