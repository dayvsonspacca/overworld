using Overworld.Apps.Crafter.Conversations;

namespace Overworld.Apps.Crafter.Presentation.Workbench;

public sealed class Attention
{
    public event EventHandler<Summons>? Summoned;

    public bool IsActive { get; set; }

    public void Summon(string folder, ChatId chat) => Summoned?.Invoke(this, new Summons(folder, chat));
}

public sealed record Summons(string Folder, ChatId Chat);
