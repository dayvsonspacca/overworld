namespace Overworld.Apps.Allay.Presentation;

public sealed record LimitRow(string Name, int Percent, string? Resets, bool Close)
{
    public string Used => $"{Percent}%";

    public double Fill => Math.Clamp(Percent, 0, 100);
}
