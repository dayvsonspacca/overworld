using Overworld.Allay.Conversations;
using Overworld.Allay.Settings;
using Overworld.Apps.Crafter.Presentation.Conversations;
using Overworld.Allay.Presentation.Conversations;

namespace Overworld.Apps.Crafter.Presentation.Tests.Conversations;

public sealed class ConversationViewModelTest : IDisposable
{
    private readonly Bench _bench = new();

    public void Dispose() => _bench.Dispose();

    private async Task<(string Folder, ConversationViewModel Conversation)> ReadyAsync()
    {
        var folder = await _bench.ProjectAsync("alpha");
        var conversation = new ConversationViewModel(folder, Bench.Chat("New chat"), _bench.Abilities);
        await conversation.LoadDeskAsync(Bench.Cancellation);

        return (folder, conversation);
    }

    [Fact]
    public async Task SetsItsDeskFromTheAgentsSetup()
    {
        await _bench.Crew.SetUpAsync("scripted", Mode.Plan, "fast", Effort.Max, Bench.Cancellation);

        var (_, conversation) = await ReadyAsync();

        Assert.Equal(("Scripted", "Plan", "Fast", "Max effort"), (conversation.Agent, conversation.ModeLabel, conversation.ModelLabel, conversation.EffortLabel));
        Assert.Equal(["default", "fast"], conversation.Models.Select(option => option.Id));
        Assert.Equal("Ask Scripted", conversation.Placeholder);
    }

    [Fact]
    public async Task SendsWhatWasTypedWithItsSetupAndKeepsTheChat()
    {
        var (folder, conversation) = await ReadyAsync();
        _bench.Session.Script(new Speech("Olá"), new Finished());
        var answered = 0;
        conversation.Answered += (_, _) => answered++;
        conversation.CycleModeCommand.Execute(null);
        conversation.PickModelCommand.Execute(conversation.Models[1]);
        conversation.ToggleEffortCommand.Execute(null);
        conversation.Draft = "  oi  ";

        await conversation.SendCommand.ExecuteAsync(null);

        Assert.Equal("oi", Assert.Single(_bench.Session.Heard).Text);
        Assert.Equal(new Setup(Mode.Edits, "fast", Effort.Max), Assert.Single(_bench.Session.Setups));
        Assert.Equal(string.Empty, conversation.Draft);
        Assert.Equal("oi", conversation.Title);
        Assert.False(conversation.IsWorking);
        Assert.Equal(1, answered);
        Assert.Collection(
            conversation.Timeline.Entries,
            entry => Assert.IsType<PromptEntry>(entry),
            entry => Assert.Equal("Olá", Assert.IsType<SpeechEntry>(entry).Text),
            entry => Assert.IsType<OutcomeEntry>(entry));
        var kept = Assert.Single((await _bench.Chats.LoadAsync(folder, Bench.Cancellation)).Value);
        Assert.Equal(("oi", "s-1", 2), (kept.Title.Text, kept.SessionId, kept.Turns.Count));
    }

    [Fact]
    public async Task QueuesWhatIsSentWhileWorkingAndSendsItAfter()
    {
        var (_, conversation) = await ReadyAsync();
        _bench.Session.Script(new Speech("First"), new Finished());
        _bench.Session.Script(new Speech("Second"), new Finished());
        var held = _bench.Session.Held = new TaskCompletionSource();
        conversation.Draft = "one";
        var talking = conversation.SendCommand.ExecuteAsync(null);
        Assert.True(conversation.IsWorking);

        while (_bench.Session.Heard.Count == 0)
        {
            await Task.Delay(10, Bench.Cancellation);
        }

        Assert.Equal("one", conversation.Title);

        conversation.Draft = "two";
        await conversation.SendCommand.ExecuteAsync(null);
        Assert.Equal("1 message waiting", conversation.QueuedLabel);

        held.SetResult();
        await talking;

        Assert.Equal(["one", "two"], _bench.Session.Heard.Select(prompt => prompt.Text));
        Assert.False(conversation.HasQueued);
    }

    [Fact]
    public async Task StopsTheReplyAndDropsWhatWasWaiting()
    {
        var (_, conversation) = await ReadyAsync();
        _bench.Session.Script(new Speech("Long"), new Finished());
        _bench.Session.Held = new TaskCompletionSource();
        conversation.Draft = "one";
        var talking = conversation.SendCommand.ExecuteAsync(null);
        conversation.Draft = "two";
        await conversation.SendCommand.ExecuteAsync(null);

        while (_bench.Session.Heard.Count == 0)
        {
            await Task.Delay(10, Bench.Cancellation);
        }

        conversation.StopCommand.Execute(null);
        await talking.WaitAsync(TimeSpan.FromSeconds(5), Bench.Cancellation);

        Assert.Single(_bench.Session.Heard);
        Assert.StartsWith("Stopped · ", Assert.IsType<OutcomeEntry>(conversation.Timeline.Entries[^1]).Text);
        Assert.False(conversation.StopCommand.CanExecute(null));
    }

    [Fact]
    public async Task StopsEvenWhileTheChatIsStillOpening()
    {
        var (_, conversation) = await ReadyAsync();
        _bench.Session.Script(new Speech("Long"), new Finished());
        _bench.Session.Held = new TaskCompletionSource();
        conversation.Draft = "one";
        var talking = conversation.SendCommand.ExecuteAsync(null);

        conversation.StopCommand.Execute(null);
        await talking.WaitAsync(TimeSpan.FromSeconds(5), Bench.Cancellation);

        Assert.False(conversation.IsWorking);
        Assert.True(_bench.Session.Heard.Count == 0 || conversation.Timeline.Entries[^1] is OutcomeEntry { Text: "Stopped" });
    }

    [Fact]
    public async Task HandsADecisionToTheSession()
    {
        var (_, conversation) = await ReadyAsync();
        _bench.Session.Script(new Permission("p-1", ActKind.Command, "dotnet test", "To check", [new Allowance("dotnet test:*", Reach.Folder)]), new Finished());
        conversation.Draft = "test it";
        await conversation.SendCommand.ExecuteAsync(null);
        var permission = Assert.IsType<PermissionEntry>(conversation.Timeline.Entries[1]);

        permission.AllowAlwaysCommand.Execute(null);
        permission.DenyCommand.Execute(null);

        Assert.Equal(("p-1", (Decision)new AllowAlways()), Assert.Single(_bench.Session.Decisions));
        Assert.Equal("Always allowed: dotnet test:*", permission.Answer);
        Assert.False(permission.IsOpen);
    }

    [Fact]
    public async Task HearsTheAgentWakeOnItsOwn()
    {
        var (folder, conversation) = await ReadyAsync();
        _bench.Session.Script(new Speech("Started"), new Finished());
        conversation.Draft = "build in the background";
        await conversation.SendCommand.ExecuteAsync(null);
        var answered = new TaskCompletionSource();
        conversation.Answered += (_, _) => answered.TrySetResult();

        _bench.Session.Wake(new Speech("The build passed."), new Finished());
        await answered.Task.WaitAsync(TimeSpan.FromSeconds(5), Bench.Cancellation);

        Assert.Equal("The build passed.", conversation.Timeline.Entries.OfType<SpeechEntry>().Last().Text);
        Assert.IsType<Reply>(Assert.Single((await _bench.Chats.LoadAsync(folder, Bench.Cancellation)).Value).Turns[^1]);
        Assert.Equal(3, Assert.Single((await _bench.Chats.LoadAsync(folder, Bench.Cancellation)).Value).Turns.Count);
    }

    [Fact]
    public async Task CannotSendWithoutAnAgentToTalkTo()
    {
        var folder = await _bench.ProjectAsync("alpha");
        var conversation = new ConversationViewModel(folder, new Apps.Crafter.Conversations.ChatRecord(Apps.Crafter.Conversations.ChatId.New(), new Allay.Agents.AgentId("gone"), Apps.Crafter.Conversations.ChatTitle.Untitled, null, []), _bench.Abilities);

        await conversation.LoadDeskAsync(Bench.Cancellation);
        conversation.Draft = "oi";

        Assert.False(conversation.SendCommand.CanExecute(null));
        Assert.NotNull(conversation.Problem);
        Assert.Equal("No agent to talk to", conversation.Placeholder);
    }

    [Fact]
    public async Task SendsWhatIsAttachedAndPointsAtTheFilesMentioned()
    {
        var (folder, conversation) = await ReadyAsync();
        File.WriteAllText(Path.Combine(folder, "notes.md"), "");
        _bench.Session.Script(new Finished());
        var picture = new Picture("image/png", [1]);
        conversation.Attach(picture);
        conversation.Attach(picture);
        conversation.Draft = "Read @notes.md";

        await conversation.SendCommand.ExecuteAsync(null);

        Assert.Equal([new FileMention("notes.md"), picture], Assert.Single(_bench.Session.Heard).Attachments);
        Assert.False(conversation.HasAttachments);
    }

    [Fact]
    public async Task SendsAPictureAloneAndDropsOneOnRequest()
    {
        var (_, conversation) = await ReadyAsync();
        conversation.Attach(new FileMention("/tmp/a.log"));
        conversation.Attach(new Picture("image/png", [1]));

        conversation.DetachCommand.Execute(conversation.Attachments[0]);

        Assert.True(conversation.SendCommand.CanExecute(null));
        Assert.True(Assert.Single(conversation.Attachments).IsPicture);
    }

    [Fact]
    public async Task RefusesACommandThatIsNotTheProjectsAndKeepsTheDraft()
    {
        var (_, conversation) = await ReadyAsync();
        conversation.Draft = "/compact";

        await conversation.SendCommand.ExecuteAsync(null);

        Assert.Empty(_bench.Session.Heard);
        Assert.Equal("/compact", conversation.Draft);
        Assert.Contains("/compact", conversation.Problem, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WithdrawsWhatWasStoppedBeforeTheAgentSaidAnythingAndBringsItBack()
    {
        var (folder, conversation) = await ReadyAsync();
        _bench.Session.Script(new Finished());
        _bench.Session.Held = new TaskCompletionSource();
        var picture = new Picture("image/png", [1]);
        conversation.Draft = "one";
        conversation.Attach(picture);
        var talking = conversation.SendCommand.ExecuteAsync(null);
        conversation.Draft = "two";
        await conversation.SendCommand.ExecuteAsync(null);

        while (_bench.Session.Heard.Count == 0)
        {
            await Task.Delay(10, Bench.Cancellation);
        }

        conversation.Draft = "three";
        conversation.StopCommand.Execute(null);
        await talking.WaitAsync(TimeSpan.FromSeconds(5), Bench.Cancellation);

        Assert.Equal("one\n\ntwo\n\nthree", conversation.Draft);
        Assert.Equal(picture, Assert.Single(conversation.Attachments).Attachment);
        Assert.Empty(conversation.Timeline.Entries);
        Assert.Equal("New chat", conversation.Title);
        Assert.Empty(Assert.Single((await _bench.Chats.LoadAsync(folder, Bench.Cancellation)).Value).Turns);
    }

    [Fact]
    public async Task KeepsWhatWasStoppedOnceTheAgentSaidSomething()
    {
        var (_, conversation) = await ReadyAsync();
        _bench.Session.Script(new Speech("Long"), new Finished());
        _bench.Session.Held = new TaskCompletionSource();
        conversation.Draft = "one";
        var talking = conversation.SendCommand.ExecuteAsync(null);
        conversation.Draft = "two";
        await conversation.SendCommand.ExecuteAsync(null);

        while (!conversation.Timeline.Entries.OfType<SpeechEntry>().Any())
        {
            await Task.Delay(10, Bench.Cancellation);
        }

        conversation.Draft = "three";
        conversation.StopCommand.Execute(null);
        await talking.WaitAsync(TimeSpan.FromSeconds(5), Bench.Cancellation);

        Assert.Equal("two\n\nthree", conversation.Draft);
        Assert.Collection(
            conversation.Timeline.Entries,
            entry => Assert.Equal("one", Assert.IsType<PromptEntry>(entry).Text),
            entry => Assert.Equal("Long", Assert.IsType<SpeechEntry>(entry).Text),
            entry => Assert.IsType<OutcomeEntry>(entry));
    }

    [Fact]
    public async Task ClearsTheMessageBox()
    {
        var (_, conversation) = await ReadyAsync();
        conversation.Draft = "oi";
        conversation.Attach(new Picture("image/png", [1]));

        conversation.ClearCommand.Execute(null);

        Assert.Equal(string.Empty, conversation.Draft);
        Assert.False(conversation.HasAttachments);
    }
}
