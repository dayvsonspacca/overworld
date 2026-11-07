namespace Overworld.Bell;

public sealed record Question(string Header, string Text, bool Several, IReadOnlyList<Choice> Choices);

public sealed record Choice(string Label, string? Detail);
