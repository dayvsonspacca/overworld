namespace Overworld.Allay.Conversations;

public sealed record Picture(string MediaType, byte[] Data) : Attachment
{
    public bool Equals(Picture? other) =>
        other is not null && MediaType == other.MediaType && Data.AsSpan().SequenceEqual(other.Data);

    public override int GetHashCode() => HashCode.Combine(MediaType, Data.Length);
}
