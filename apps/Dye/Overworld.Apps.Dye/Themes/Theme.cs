namespace Overworld.Apps.Dye.Themes;

public sealed record Theme(string Name, bool Active, IReadOnlyDictionary<string, string> Colors);
