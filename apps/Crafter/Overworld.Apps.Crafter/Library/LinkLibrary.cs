using System.ComponentModel;

namespace Overworld.Apps.Crafter.Library;

public sealed record LinkLibrary(
    [property: Description("The repository that keeps the skills, as git clones it. A blank one unlinks the library.")]
    string Repository);
