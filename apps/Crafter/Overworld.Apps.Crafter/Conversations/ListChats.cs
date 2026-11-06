using System.ComponentModel;

namespace Overworld.Apps.Crafter.Conversations;

public sealed record ListChats(
    [property: Description("The folder of a project open in Crafter.")]
    string Folder);
