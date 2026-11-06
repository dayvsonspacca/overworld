namespace Overworld.Allay.Settings;

public sealed class Configuration : IEquatable<Configuration>
{
    public const string Cleared = "(cleared)";

    public static readonly Configuration Empty = new(new Dictionary<string, string>());

    private readonly Dictionary<string, string> _values;

    public Configuration(IReadOnlyDictionary<string, string> values)
    {
        _values = new Dictionary<string, string>(values.Where(value => value.Value.Trim().Length > 0), StringComparer.Ordinal);
    }

    public IReadOnlyDictionary<string, string> Values => _values;

    public string? this[string key] => _values.GetValueOrDefault(key);

    public Configuration With(string key, string? value)
    {
        var changed = new Dictionary<string, string>(_values, StringComparer.Ordinal);

        if (value is null)
        {
            changed.Remove(key);
        }
        else
        {
            changed[key] = value;
        }

        return new Configuration(changed);
    }

    public Configuration Over(Configuration under) =>
        new(new Dictionary<string, string>(under._values
            .Concat(_values)
            .GroupBy(value => value.Key)
            .Select(group => group.Last())
            .Where(value => value.Value != Cleared)));

    public bool Equals(Configuration? other) =>
        other is not null && _values.Count == other._values.Count && _values.All(value => other[value.Key] == value.Value);

    public override bool Equals(object? obj) => Equals(obj as Configuration);

    public override int GetHashCode() => _values.Count;
}
