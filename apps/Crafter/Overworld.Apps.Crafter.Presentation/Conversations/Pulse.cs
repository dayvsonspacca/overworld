using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using Overworld.Allay.Conversations;

namespace Overworld.Apps.Crafter.Presentation.Conversations;

[SuppressMessage(
    "Reliability",
    "CA1001",
    Justification = "The timer lives only while the agent works and is disposed when it stops.")]
public sealed partial class Pulse(TimeProvider? clock = null) : ObservableObject
{
    private const string Thinking = "Thinking…";

    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    private readonly SynchronizationContext? _ui = SynchronizationContext.Current;

    private ITimer? _timer;

    private DateTimeOffset _since;

    [ObservableProperty]
    public partial bool IsShown { get; private set; }

    [ObservableProperty]
    public partial string Word { get; private set; } = Thinking;

    [ObservableProperty]
    public partial string Elapsed { get; private set; } = string.Empty;

    public void Start()
    {
        _since = _clock.GetUtcNow();
        Word = Thinking;
        IsShown = true;
        Tick();
        _timer ??= _clock.CreateTimer(_ => OnUi(Tick), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    public void Heard(Moment moment)
    {
        if (_timer is null)
        {
            return;
        }

        switch (moment)
        {
            case Request:
                IsShown = false;
                break;
            case Speech:
                Show("Writing…");
                break;
            case Act:
                Show("Working…");
                break;
            case Thought or ActResult or Withdrawn:
                Show(Thinking);
                break;
        }
    }

    public void Resume()
    {
        if (_timer is not null)
        {
            Show(Thinking);
        }
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
        IsShown = false;
    }

    public void Tick() => Elapsed = ThoughtEntry.Spoken(_clock.GetUtcNow() - _since);

    private void Show(string word)
    {
        Word = word;
        IsShown = true;
    }

    private void OnUi(Action act)
    {
        if (_ui is null)
        {
            act();
        }
        else
        {
            _ui.Post(_ => act(), null);
        }
    }
}
