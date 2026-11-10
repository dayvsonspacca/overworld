using CommunityToolkit.Mvvm.ComponentModel;
using Overworld.Apps.Crafter.Presentation.Chats;
using Overworld.Apps.Crafter.Presentation.Projects;

namespace Overworld.Apps.Crafter.Presentation.Home;

public sealed class RecentChatViewModel(ProjectViewModel project, ChatSummaryViewModel chat) : ObservableObject
{
    public ProjectViewModel Project { get; } = project;

    public ChatSummaryViewModel Chat { get; } = chat;
}
