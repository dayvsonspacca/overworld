using System.Collections.Concurrent;
using Overworld.Allay.Briefs;
using Overworld.Allay.Agents;
using Overworld.Allay.Conversations;
using Overworld.Allay.Hiring;
using Overworld.Allay.Remote;
using Overworld.Allay.Settings;
using Overworld.Apps.Crafter.Conversations;
using Overworld.Apps.Crafter.Proposals;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Sessions;

public sealed class Workshop(Crew crew, ProjectBrief briefs, Toolbox toolbox, ChatStore store, AllayLink? allay = null, Func<string, ChatId, IReadOnlyList<string>?>? opener = null)
{
    private readonly ConcurrentDictionary<ChatId, Open> _open = new();

    public event EventHandler<string>? Changed;

    public bool IsServed => allay?.IsServing() == true;

    public async Task<Result<ChatRecord>> FreshAsync(CancellationToken cancellationToken)
    {
        var hired = await crew.HiredAsync(cancellationToken);

        if (!hired.IsSuccess)
        {
            return hired.Error;
        }

        return hired.Value is [var agent, ..]
            ? new ChatRecord(ChatId.New(), agent.Id, ChatTitle.Untitled, null, [])
            : new Error("crafter.no_agent", "No agent is hired yet. Hire one in Allay, or with: overworld:allay agents hire --agent claude-code");
    }

    public async Task<Result<Desk>> DeskAsync(ChatRecord chat, CancellationToken cancellationToken)
    {
        var agent = crew.Find(chat.Agent.Value);

        if (!agent.IsSuccess)
        {
            return agent.Error;
        }

        var setup = await crew.SetupOfAsync(chat.Agent.Value, cancellationToken);

        return setup.IsSuccess
            ? new Desk(agent.Value.Name, agent.Value.Color, setup.Value, await agent.Value.ModelsAsync(cancellationToken))
            : setup.Error;
    }

    public async Task<Result<Chat>> OpenAsync(string folder, ChatRecord record, CancellationToken cancellationToken) =>
        await OpenAsync(folder, record, null, cancellationToken);

    public async Task<Result<Chat>> OpenAsync(string folder, ChatRecord record, Asker? ask, CancellationToken cancellationToken)
    {
        if (_open.TryGetValue(record.Id, out var open))
        {
            return open.Chat;
        }

        var agent = crew.Find(record.Agent.Value);

        if (!agent.IsSuccess)
        {
            return agent.Error;
        }

        Chat? chat = null;
        var tools = toolbox.Admit(folder, ask, () => _ = KeptAsync(folder), title => chat?.Rename(title));
        var brief = await briefs.OfAsync(folder, tools, cancellationToken);

        if (!brief.IsSuccess)
        {
            toolbox.Release(tools);

            return brief.Error;
        }

        var (session, turns) = await BeginAsync(folder, agent.Value, record, brief.Value, cancellationToken);
        chat = new Chat(record.Id, record.Agent, record.Title, new Conversation(session, turns));
        _open[record.Id] = new Open(folder, chat, tools);

        return chat;
    }

    public Task<Result<IReadOnlyList<ChatRecord>>> KeepAsync(string folder, Chat chat, CancellationToken cancellationToken) =>
        store.KeepAsync(folder, chat.Record(), cancellationToken);

    public async Task RebriefAsync(string folder, CancellationToken cancellationToken)
    {
        foreach (var open in _open.Values.Where(open => open.Folder == folder))
        {
            var brief = await briefs.OfAsync(folder, open.Tools, cancellationToken);

            if (brief.IsSuccess)
            {
                open.Chat.Session.Rebrief(brief.Value);
            }
        }
    }

    public void Close(ChatId chat)
    {
        if (_open.TryRemove(chat, out var open))
        {
            open.Chat.Session.End();
            toolbox.Release(open.Tools);
        }
    }

    public void CloseAll()
    {
        foreach (var chat in _open.Keys)
        {
            Close(chat);
        }
    }

    public void LeaveAll()
    {
        foreach (var chat in _open.Keys)
        {
            if (!_open.TryRemove(chat, out var open))
            {
                continue;
            }

            if (open.Chat.Session is RemoteSession remote)
            {
                remote.Leave();
            }
            else
            {
                open.Chat.Session.End();
            }

            toolbox.Release(open.Tools);
        }
    }

    private async Task<(ISession Session, IReadOnlyList<Turn> Turns)> BeginAsync(string folder, IAgent agent, ChatRecord record, Brief brief, CancellationToken cancellationToken)
    {
        if (allay is { } link && link.IsServing())
        {
            var opened = await link.OpenAsync(record.Id.Value.ToString(), record.Agent.Value, brief, record.SessionId, record.Turns, keep: true, cancellationToken, opener?.Invoke(folder, record.Id));

            if (opened.IsSuccess)
            {
                return (opened.Value.Session, opened.Value.Turns);
            }
        }

        return (agent.Begin(brief, record.SessionId), record.Turns);
    }

    public async Task KeptEverywhereAsync()
    {
        foreach (var folder in _open.Values.Select(open => open.Folder).Distinct().ToArray())
        {
            await KeptAsync(folder);
        }
    }

    private async Task KeptAsync(string folder)
    {
        await RebriefAsync(folder, CancellationToken.None);
        Changed?.Invoke(this, folder);
    }

    private sealed record Open(string Folder, Chat Chat, IReadOnlyList<ToolServer> Tools);
}
