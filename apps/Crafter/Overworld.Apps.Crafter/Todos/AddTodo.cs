using System.ComponentModel;

namespace Overworld.Apps.Crafter.Todos;

public sealed record AddTodo(
    [property: Description("The folder of a project open in Crafter.")]
    string Folder,
    [property: Description("What is left to do, in a few words.")]
    string Text);
