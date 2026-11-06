using System.ComponentModel;

namespace Overworld.Apps.Crafter.Conversations;

public sealed record RenameChat(
    [property: Description("The chat's new title: a few words on what it is about, like \"Pictures in the conversation\".")]
    string Title);
