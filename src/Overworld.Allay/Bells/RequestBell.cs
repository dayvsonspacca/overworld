using Overworld.Allay.Conversations;
using Overworld.Bell;
using AllayQuestions = Overworld.Allay.Conversations.Questions;
using BellQuestion = Overworld.Bell.Question;
using BellQuestions = Overworld.Bell.Questions;
using Reply = Overworld.Bell.Reply;

namespace Overworld.Allay.Bells;

public static class RequestBell
{
    private const string Once = "allowOnce";
    private const string Always = "allowAlways";
    private const string Never = "deny";

    public static RingBell Ring(Request request, string from, string context, string? color, bool quiet) => request switch
    {
        Permission permission => new RingBell(from, Asks(permission.Kind), permission.Reason, context, color, Buttons(permission), quiet),
        AllayQuestions questions => new RingBell(from, "A question", null, context, color, Asked(questions), quiet),
        _ => throw new ArgumentOutOfRangeException(nameof(request), request, "Only permissions and questions ring the Bell."),
    };

    public static Decision Decision(Request request, Reply reply) => (request, reply) switch
    {
        (Permission, Pressed { Key: Once }) => new AllowOnce(),
        (Permission, Pressed { Key: Always }) => new AllowAlways(),
        (Permission, Pressed) => new Deny(),
        (AllayQuestions questions, Answered answered) => new Answers([.. questions.Asked.Zip(answered.Given, Answer)]),
        _ => new Skip(),
    };

    public static string Asks(ActKind kind) => kind switch
    {
        ActKind.Command => "Run a command?",
        ActKind.Change => "Change a file?",
        ActKind.Create => "Create a file?",
        ActKind.Read => "Read a file?",
        ActKind.Browse => "Open a page?",
        _ => "Go ahead?",
    };

    private static Buttons Buttons(Permission permission) => new(
        permission.Subject,
        [
            new Button(Never, "Deny", Tone.Danger),
            .. permission.Remembers.Count > 0 ? [new Button(Always, "Always allow")] : Array.Empty<Button>(),
            new Button(Once, "Allow once", Tone.Primary),
        ]);

    private static BellQuestions Asked(AllayQuestions questions) => new(
        [.. questions.Asked.Select(question => new BellQuestion(
            question.Header,
            question.Text,
            question.Several,
            [.. question.Options.Select(option => new Choice(option.Label, option.Description))]))]);

    private static QuestionAnswer Answer(Overworld.Allay.Conversations.Question question, Picked picked) =>
        new(question, [.. question.Options.Where(option => picked.Labels.Contains(option.Label))], picked.Written);
}
