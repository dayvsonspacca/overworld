using System.Text.Json.Nodes;
using Overworld.Web.Mirroring;

namespace Overworld.Web;

public sealed class Page(Mirror mirror, Loop loop)
{
    private readonly Dictionary<string, Func<JsonNode?, Task<JsonNode?>>> _handlers = [];

    private bool _isActive;

    public event EventHandler<PageEvent>? Emitted;

    public event EventHandler? Activated;

    public event EventHandler? Deactivated;

    public Mirror Mirror { get; } = mirror;

    public Loop Loop { get; } = loop;

    public ICollection<Func<CancellationToken, Task>> Leaving { get; } = [];

    public bool IsActive
    {
        get => _isActive;
        internal set
        {
            if (_isActive == value)
            {
                return;
            }

            _isActive = value;
            (value ? Activated : Deactivated)?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Show(object root) => Mirror.Show(root);

    public void Handle(string name, Func<JsonNode?, Task<JsonNode?>> handler) => _handlers[name] = handler;

    public void Handle(string name, Action<JsonNode?> handler) =>
        _handlers[name] = argument =>
        {
            handler(argument);

            return Task.FromResult<JsonNode?>(null);
        };

    public void Emit(string name, JsonNode? data = null) => Emitted?.Invoke(this, new PageEvent(name, data));

    public void Summon() => Emit("summon");

    internal Task<JsonNode?> AskAsync(string name, JsonNode? argument) =>
        _handlers.TryGetValue(name, out var handler) ? handler(argument) : Task.FromResult<JsonNode?>(null);
}
