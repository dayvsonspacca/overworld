using System.ComponentModel;

namespace Overworld.Apps.Crafter.Conversations;

public sealed record ReadChat(
    [property: Description("The folder of a project open in Crafter.")]
    string Folder,
    [property: Description("The chat's id, as list-chats gives it.")]
    Guid Chat);
