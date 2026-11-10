using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace Overworld.Web.Mirroring;

public sealed class Mirror
{
    private const int Deepest = 8;

    private const BindingFlags Lookup = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private readonly Dictionary<object, int> _ids = new(ReferenceEqualityComparer.Instance);

    private readonly Dictionary<int, object> _objects = [];

    private readonly HashSet<(Type Type, string Property)> _ignored = [];

    private readonly HashSet<int> _defined = [];

    private readonly HashSet<(int Id, string Property)> _changed = [];

    private readonly HashSet<int> _relisted = [];

    private readonly List<JsonNode> _outbox = [];

    private int _next;

    public event EventHandler? Changed;

    public object? Root { get; private set; }

    public void Show(object root)
    {
        Root = root;
        Encode(root, 0);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Ignore<T>(params string[] properties) => Ignore(typeof(T), properties);

    public void Ignore(Type type, params string[] properties)
    {
        foreach (var property in properties)
        {
            _ignored.Add((type, property));
        }
    }

    public JsonArray Whole()
    {
        _outbox.Clear();
        _changed.Clear();
        _relisted.Clear();
        _defined.Clear();

        var root = Root is null ? null : Encode(Root, 0);
        var batch = Drain();
        batch.Add(new JsonObject { ["t"] = "root", ["value"] = root });

        return batch;
    }

    public JsonArray Drain()
    {
        foreach (var id in _relisted.ToList())
        {
            if (_objects.TryGetValue(id, out var list))
            {
                _outbox.Add(new JsonObject { ["t"] = "list", ["id"] = id, ["items"] = Items((IEnumerable)list, 0) });
            }
        }

        _relisted.Clear();

        foreach (var (id, property) in _changed.ToList())
        {
            if (_objects.TryGetValue(id, out var target))
            {
                _outbox.Add(Change(id, target, property));
            }
        }

        _changed.Clear();

        var batch = new JsonArray([.. _outbox]);
        _outbox.Clear();

        return batch;
    }

    public object? Find(int id) => _objects.GetValueOrDefault(id);

    public object? Decode(JsonNode? node, Type type)
    {
        if (node is JsonObject { } reference && reference["$ref"] is { } id)
        {
            return Find(id.GetValue<int>());
        }

        if (node is null)
        {
            return null;
        }

        if (type == typeof(object))
        {
            return node.GetValueKind() switch
            {
                JsonValueKind.String => node.GetValue<string>(),
                JsonValueKind.Number => node.GetValue<double>(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => node.ToJsonString(),
            };
        }

        if (node is JsonObject plain && plain.ContainsKey("$type"))
        {
            var bare = (JsonObject)plain.DeepClone();
            bare.Remove("$type");
            node = bare;
        }

        return node.Deserialize(type, Json);
    }

    public void Call(int id, string command, JsonNode? argument)
    {
        if (Find(id) is not { } target
            || target.GetType().GetProperty(command, Lookup)?.GetValue(target) is not ICommand callable)
        {
            return;
        }

        var parameter = Decode(argument, Parameter(callable));

        if (callable.CanExecute(parameter))
        {
            callable.Execute(parameter);
        }
    }

    public void Set(int id, string property, JsonNode? value)
    {
        if (Find(id) is not { } target
            || target.GetType().GetProperty(property, Lookup) is not { CanWrite: true } found
            || found.SetMethod is not { IsPublic: true })
        {
            return;
        }

        found.SetValue(target, Decode(value, found.PropertyType));
    }

    public async Task<JsonNode?> InvokeAsync(int id, string method, JsonArray arguments)
    {
        if (Find(id) is not { } target)
        {
            return null;
        }

        var chosen = target.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(each => string.Equals(each.Name, method, StringComparison.OrdinalIgnoreCase)
                && !each.IsSpecialName
                && each.DeclaringType != typeof(object)
                && each.GetParameters().Count(parameter => parameter.ParameterType != typeof(CancellationToken)) == arguments.Count);

        if (chosen is null)
        {
            return null;
        }

        var given = 0;
        var values = chosen.GetParameters()
            .Select(parameter => parameter.ParameterType == typeof(CancellationToken) ? CancellationToken.None : Decode(arguments[given++], parameter.ParameterType))
            .ToArray();
        var result = chosen.Invoke(target, values);

        if (result is Task task)
        {
            await task;

            result = task.GetType().IsGenericType ? task.GetType().GetProperty("Result")?.GetValue(task) : null;
        }

        var encoded = Encode(result, 0);
        Changed?.Invoke(this, EventArgs.Empty);

        return encoded;
    }

    private static Type Parameter(ICommand command) =>
        command.GetType().GetInterfaces()
            .FirstOrDefault(each => each.IsGenericType && each.GetGenericTypeDefinition().Name.StartsWith("IRelayCommand", StringComparison.Ordinal))
            ?.GetGenericArguments()[0]
        ?? typeof(object);

    private JsonObject Change(int id, object target, string property)
    {
        var found = target.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance);
        var value = Read(target, found);

        if (value is ICommand command)
        {
            return new JsonObject { ["t"] = "can", ["id"] = id, ["cmd"] = property, ["value"] = command.CanExecute(null) };
        }

        return new JsonObject { ["t"] = "set", ["id"] = id, ["prop"] = Camel(property), ["value"] = Encode(value, 0) };
    }

    private static object? Read(object target, PropertyInfo? property)
    {
        if (property is null || property.GetIndexParameters().Length > 0 || property.GetMethod is not { IsPublic: true })
        {
            return null;
        }

        try
        {
            return property.GetValue(target);
        }
        catch (TargetInvocationException)
        {
            return null;
        }
    }

    private JsonNode? Encode(object? value, int depth)
    {
        switch (value)
        {
            case null:
                return null;
            case string text:
                return text;
            case bool flag:
                return flag;
            case Enum named:
                return named.ToString();
            case byte[] bytes:
                return new JsonObject { ["$blob"] = Register(bytes) };
            case DateTimeOffset moment:
                return moment.ToString("O", CultureInfo.InvariantCulture);
            case DateTime moment:
                return moment.ToString("O", CultureInfo.InvariantCulture);
            case Guid or Uri or char:
                return Convert.ToString(value, CultureInfo.InvariantCulture);
            case TimeSpan span:
                return span.TotalSeconds;
            case ICommand:
                return null;
            case ITuple tuple:
                return new JsonArray([.. Enumerable.Range(0, tuple.Length).Select(at => Encode(tuple[at], depth + 1))]);
        }

        var type = value.GetType();

        if (type.IsPrimitive || value is decimal)
        {
            return JsonValue.Create(Convert.ToDouble(value, CultureInfo.InvariantCulture));
        }

        if (value is INotifyPropertyChanged or INotifyCollectionChanged)
        {
            return new JsonObject { ["$ref"] = Track(value) };
        }

        if (depth > Deepest)
        {
            return null;
        }

        if (value is IDictionary dictionary)
        {
            var map = new JsonObject();

            foreach (DictionaryEntry entry in dictionary)
            {
                map[Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty] = Encode(entry.Value, depth + 1);
            }

            return map;
        }

        if (value is IEnumerable items)
        {
            return Items(items, depth);
        }

        return Plain(value, depth);
    }

    private JsonArray Items(IEnumerable items, int depth) => [.. items.Cast<object?>().Select(item => Encode(item, depth + 1))];

    private JsonObject Plain(object value, int depth)
    {
        var plain = new JsonObject { ["$type"] = value.GetType().Name, ["$id"] = Register(value) };

        foreach (var property in Properties(value.GetType()))
        {
            if (!typeof(ICommand).IsAssignableFrom(property.PropertyType))
            {
                plain[Camel(property.Name)] = Encode(Read(value, property), depth + 1);
            }
        }

        return plain;
    }

    private int Register(object value)
    {
        if (!_ids.TryGetValue(value, out var id))
        {
            id = ++_next;
            _ids[value] = id;
            _objects[id] = value;
        }

        return id;
    }

    private int Track(object value)
    {
        if (_ids.TryGetValue(value, out var id))
        {
            if (_defined.Add(id))
            {
                Define(id, value);
            }

            return id;
        }

        id = Register(value);
        _defined.Add(id);

        if (value is INotifyPropertyChanged notifying)
        {
            notifying.PropertyChanged += (_, change) => Touch(id, value, change.PropertyName);
        }

        if (value is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged += (_, _) =>
            {
                _relisted.Add(id);
                Changed?.Invoke(this, EventArgs.Empty);
            };
        }

        foreach (var property in Properties(value.GetType()))
        {
            if (Read(value, property) is ICommand command)
            {
                var name = property.Name;
                command.CanExecuteChanged += (_, _) =>
                {
                    _changed.Add((id, name));
                    Changed?.Invoke(this, EventArgs.Empty);
                };
            }
        }

        Define(id, value);

        return id;
    }

    private void Define(int id, object value)
    {
        if (value is INotifyCollectionChanged and IEnumerable list)
        {
            _outbox.Add(new JsonObject { ["t"] = "list", ["id"] = id, ["items"] = Items(list, 0) });

            return;
        }

        var props = new JsonObject();
        var commands = new JsonObject();

        foreach (var property in Properties(value.GetType()))
        {
            var read = Read(value, property);

            if (read is ICommand command)
            {
                commands[property.Name] = command.CanExecute(null);
            }
            else
            {
                props[Camel(property.Name)] = Encode(read, 0);
            }
        }

        _outbox.Add(new JsonObject
        {
            ["t"] = "obj",
            ["id"] = id,
            ["type"] = value.GetType().Name,
            ["types"] = new JsonArray([.. Lineage(value.GetType()).Select(type => (JsonNode)type)]),
            ["props"] = props,
            ["cmds"] = commands,
        });
    }

    private void Touch(int id, object value, string? property)
    {
        if (string.IsNullOrEmpty(property))
        {
            foreach (var each in Properties(value.GetType()))
            {
                _changed.Add((id, each.Name));
            }
        }
        else if (!_ignored.Contains((value.GetType(), property)) && value.GetType().GetProperty(property) is not null)
        {
            _changed.Add((id, property));
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private static bool Mirrorable(Type type) =>
        !typeof(Task).IsAssignableFrom(type)
        && !typeof(Delegate).IsAssignableFrom(type)
        && !typeof(Stream).IsAssignableFrom(type)
        && !typeof(MemberInfo).IsAssignableFrom(type)
        && type != typeof(CancellationToken);

    private IEnumerable<PropertyInfo> Properties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetIndexParameters().Length == 0
                && Mirrorable(property.PropertyType)
                && property.GetMethod is { IsPublic: true }
                && !_ignored.Contains((type, property.Name))
                && !_ignored.Contains((property.DeclaringType!, property.Name)));

    private static IEnumerable<string> Lineage(Type type)
    {
        for (var each = type; each is not null && each != typeof(object); each = each.BaseType)
        {
            yield return each.Name;
        }
    }

    private static string Camel(string name) => char.ToLowerInvariant(name[0]) + name[1..];
}
