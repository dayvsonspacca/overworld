using System.Globalization;
using System.Text.Json;
using Overworld.Allay.Metering;
using static Overworld.Allay.ClaudeCode.Protocol.JsonFields;

namespace Overworld.Allay.ClaudeCode.Protocol;

internal static class UsageReport
{
    private const string Normal = "normal";

    public static Usage? Read(string output)
    {
        foreach (var line in output.Split('\n'))
        {
            if (!line.Contains("\"usage_report\"", StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(line);

                if (document.RootElement.ValueKind == JsonValueKind.Object
                    && document.RootElement.TryGetProperty("usage_report", out var report)
                    && report.ValueKind == JsonValueKind.Object)
                {
                    return report.TryGetProperty("rate_limits", out var rates) && rates.ValueKind == JsonValueKind.Object
                        ? new Usage(Limits(rates), Spent(rates))
                        : new Usage([]);
                }
            }
            catch (JsonException)
            {
            }
        }

        return null;
    }

    private static IReadOnlyList<Limit> Limits(JsonElement rates) =>
        rates.TryGetProperty("limits", out var limits) && limits.ValueKind == JsonValueKind.Array
            ? [.. limits.EnumerateArray().Select(LimitOf).OfType<Limit>()]
            : [];

    private static Limit? LimitOf(JsonElement limit) =>
        Number(limit, "percent") is { } percent
            ? new Limit(
                NameOf(limit),
                (int)Math.Round(percent, MidpointRounding.AwayFromZero),
                Moment(Text(limit, "resets_at")),
                Text(limit, "severity") is { } severity && severity != Normal)
            : null;

    private static string NameOf(JsonElement limit)
    {
        var kind = Text(limit, "kind");
        var period = Text(limit, "group") switch
        {
            "session" => "Current session",
            "weekly" => "This week",
            "monthly" => "This month",
            var other => Spoken(kind ?? other ?? "limit"),
        };
        var scope = ScopeOf(limit) ?? (kind == "weekly_all" ? "all models" : null);

        return scope is null ? period : $"{period} · {scope}";
    }

    private static string? ScopeOf(JsonElement limit)
    {
        if (!limit.TryGetProperty("scope", out var scope) || scope.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return (scope.TryGetProperty("model", out var model) ? Text(model, "display_name") : null)
            ?? Text(scope, "surface")
            ?? (scope.TryGetProperty("surface", out var surface) ? Text(surface, "display_name") : null);
    }

    private static Spending? Spent(JsonElement rates)
    {
        if (!rates.TryGetProperty("extra_usage", out var extra) || !Flag(extra, "is_enabled") || Number(extra, "used_credits") is not { } used)
        {
            return null;
        }

        return new Spending("Extra usage this month", used / 100, Text(extra, "currency") ?? "USD", Number(extra, "monthly_limit") / 100);
    }

    private static DateTimeOffset? Moment(string? text) =>
        DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var moment) ? moment : null;

    private static string Spoken(string kind) =>
        kind.Replace('_', ' ') is { Length: > 0 } words ? char.ToUpperInvariant(words[0]) + words[1..] : kind;
}
