using System.ComponentModel;

namespace Overworld.Bell;

public sealed record AnswerNotice(
    [property: Description("Id of the notice, as bell-list gives it.")]
    string Id,
    [property: Description("The answer: the choices picked for each question, the button pressed, or skipped.")]
    Reply Reply);
