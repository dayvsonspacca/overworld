using Overworld.Allay.ClaudeCode.Protocol;
using Overworld.Allay.Metering;

namespace Overworld.Allay.ClaudeCode.Tests.Protocol;

public sealed class UsageReportTest
{
    [Fact]
    public void ReadsThePlansLimits()
    {
        const string output = """
            {"type":"system","subtype":"init","session_id":"s-1"}
            {"type":"assistant","usage_report":{"rate_limits":{"limits":[{"kind":"session","group":"session","percent":59,"resets_at":"2026-11-06T21:49:59+00:00","scope":null,"severity":"normal"},{"kind":"weekly_all","group":"weekly","percent":50.5,"resets_at":"2026-11-07T09:59:59+00:00","scope":null,"severity":"warning"},{"kind":"weekly_scoped","group":"weekly","percent":0,"resets_at":null,"scope":{"model":{"display_name":"Fable"},"surface":null},"severity":"normal"}],"extra_usage":{"is_enabled":false,"monthly_limit":null,"used_credits":null,"currency":null}}}}
            """;

        Assert.Equal(
            new Usage(
            [
                new Limit("Current session", 59, new DateTimeOffset(2026, 11, 6, 21, 49, 59, TimeSpan.Zero)),
                new Limit("This week · all models", 51, new DateTimeOffset(2026, 11, 7, 9, 59, 59, TimeSpan.Zero), Close: true),
                new Limit("This week · Fable", 0),
            ]),
            UsageReport.Read(output));
    }

    [Fact]
    public void ReadsExtraUsageInTheCurrencysMinorUnits()
    {
        const string report = """{"usage_report":{"rate_limits":{"limits":[],"extra_usage":{"is_enabled":true,"monthly_limit":5000,"used_credits":1240,"currency":"EUR"}}}}""";

        Assert.Equal(new Usage([], new Spending("Extra usage this month", 12.40m, "EUR", 50m)), UsageReport.Read(report));
    }

    [Fact]
    public void TakesDollarsWhenTheCurrencyIsUnsaidAndNoCapWhenThereIsNone()
    {
        const string report = """{"usage_report":{"rate_limits":{"extra_usage":{"is_enabled":true,"monthly_limit":null,"used_credits":321,"currency":null}}}}""";

        Assert.Equal(new Usage([], new Spending("Extra usage this month", 3.21m, "USD")), UsageReport.Read(report));
    }

    [Fact]
    public void HasNoLimitsWhenThePlanHasNone()
    {
        Assert.Equal(new Usage([]), UsageReport.Read("""{"usage_report":{"session":{"total_cost_usd":0},"rate_limits":null}}"""));
    }

    [Theory]
    [InlineData("")]
    [InlineData("""{"type":"result","subtype":"success"}""")]
    [InlineData("""{"usage_report": broken""")]
    public void ReadsNothingWithoutAReport(string output)
    {
        Assert.Null(UsageReport.Read(output));
    }
}
