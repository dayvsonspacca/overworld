namespace Overworld.Apps.Painting.Presentation;

public sealed record Shelf(string Key, string Name, int Count, IReadOnlyList<Guid> Previews);
