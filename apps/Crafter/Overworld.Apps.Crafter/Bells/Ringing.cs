using Overworld.Allay.Conversations;

namespace Overworld.Apps.Crafter.Bells;

public sealed record Ringing(string Context, string? Color, bool Quiet, Action<Decision> Decide, Action Open);
