using Overworld.Allay.Conversations;

namespace Overworld.Allay.Remote;

public sealed record Transcript(string? Session, IReadOnlyList<Turn> Turns, IReadOnlyList<Moment> Pending)
{
    public IReadOnlyList<Turn> Settled()
    {
        if (Pending.Count == 0)
        {
            return Turns;
        }

        List<Moment> kept = [];

        foreach (var moment in Pending)
        {
            switch (moment)
            {
                case Speech speech when kept is [.., Speech said]:
                    kept[^1] = new Speech(said.Text + speech.Text);
                    break;
                case Thought thought when kept is [.., Thought had]:
                    kept[^1] = new Thought(had.Text + thought.Text, had.Took);
                    break;
                case Outcome:
                    break;
                default:
                    kept.Add(moment);
                    break;
            }
        }

        return [.. Turns, new Reply(kept, new Failed("Allay stopped before this reply finished."))];
    }
}
