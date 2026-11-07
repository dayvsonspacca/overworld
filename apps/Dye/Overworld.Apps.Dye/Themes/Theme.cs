namespace Overworld.Apps.Dye.Themes;

public sealed record Theme(
    string Name,
    Variant? Worn,
    IReadOnlyDictionary<string, string> Dark,
    IReadOnlyDictionary<string, string> Light)
{
    public bool Active => Worn is not null;

    public IReadOnlyDictionary<string, string> Colors(Variant variant) => variant == Variant.Light ? Light : Dark;
}
