using Overworld.Allay.Conversations;

namespace Overworld.Allay.Bells;

public sealed record Ringing(string Context, string? Color, bool Quiet, Action<Decision> Decide, Action Open);
