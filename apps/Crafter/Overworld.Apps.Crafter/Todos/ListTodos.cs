using System.ComponentModel;

namespace Overworld.Apps.Crafter.Todos;

public sealed record ListTodos(
    [property: Description("The folder of a project open in Crafter.")]
    string Folder);
