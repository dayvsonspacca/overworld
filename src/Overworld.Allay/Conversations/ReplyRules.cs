namespace Overworld.Allay.Conversations;

public static class ReplyRules
{
    public static IReadOnlyList<string> Broken(IReadOnlyList<Moment> reply)
    {
        List<string> broken = [];
        HashSet<string> acts = [];
        HashSet<string> results = [];
        HashSet<string> requests = [];
        HashSet<string> settled = [];

        var outcomes = reply.Count(moment => moment is Outcome);

        if (outcomes != 1)
        {
            broken.Add($"A reply ends with exactly one outcome; this one has {outcomes}.");
        }
        else if (reply[^1] is not Outcome)
        {
            broken.Add("Nothing comes after a reply's outcome.");
        }

        foreach (var moment in reply)
        {
            switch (moment)
            {
                case Act act when !acts.Add(act.Id):
                    broken.Add($"Act {act.Id} is announced more than once.");
                    break;
                case ActResult result when !acts.Contains(result.Id):
                    broken.Add($"The result of act {result.Id} comes before the act, or without it.");
                    break;
                case ActResult result when !results.Add(result.Id):
                    broken.Add($"Act {result.Id} has more than one result.");
                    break;
                case Request request when !requests.Add(request.Id):
                    broken.Add($"Request {request.Id} is made more than once.");
                    break;
                case Withdrawn withdrawn when !requests.Contains(withdrawn.Id):
                    broken.Add($"Request {withdrawn.Id} is withdrawn without having been made.");
                    break;
                case Withdrawn withdrawn when !settled.Add(withdrawn.Id):
                    broken.Add($"Request {withdrawn.Id} is withdrawn more than once.");
                    break;
            }
        }

        return broken;
    }
}
