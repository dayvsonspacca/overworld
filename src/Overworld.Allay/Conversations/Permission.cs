using System.Diagnostics.CodeAnalysis;

namespace Overworld.Allay.Conversations;

[SuppressMessage(
    "Naming",
    "CA1711",
    Justification = "Permission is what the agent asks for in a conversation, not a code access permission.")]
public sealed record Permission(string Id, ActKind Kind, string Subject, string? Reason, IReadOnlyList<Allowance> Remembers) : Request(Id)
{
    public bool Equals(Permission? other) =>
        other is not null && Id == other.Id && Kind == other.Kind && Subject == other.Subject && Reason == other.Reason
        && Remembers.SequenceEqual(other.Remembers);

    public override int GetHashCode() => HashCode.Combine(Id, Kind, Subject, Remembers.Count);
}
