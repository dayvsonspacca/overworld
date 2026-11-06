namespace Overworld.Allay.Settings;

public sealed record Entry(string Key, string Title, string About, string Fallback) : Setting(Key, Title, About)
{
    public string Value(Configuration configuration) => configuration[Key] ?? Fallback;
}
