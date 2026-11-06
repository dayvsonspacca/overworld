using System.ComponentModel;

namespace Overworld.Apps.Crafter.Todos;

public sealed record CompleteTodo(
    [property: Description("The folder of a project open in Crafter.")]
    string Folder,
    [property: Description("The todo's number, as list-todos gives it.")]
    int Number,
    [property: Description("False marks a done todo as not done again.")]
    bool Done = true);
