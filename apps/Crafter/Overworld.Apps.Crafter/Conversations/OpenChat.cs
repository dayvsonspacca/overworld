using System.ComponentModel;

namespace Overworld.Apps.Crafter.Conversations;

public sealed record OpenChat(
    [property: Description("The folder of a project open in Crafter.")]
    string Folder,
    [property: Description("The chat's first message, sent to the project's agent as if the user typed it. Leave it out to open the chat empty, for the user to write in.")]
    string? Prompt = null);
