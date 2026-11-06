namespace Overworld.Apps.Dye.Presentation.Themes;

public sealed record RoleSwatch(string Role, string Hex)
{
    public bool IsTint => Hex.Length == 9;
}
