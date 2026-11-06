using System.Text.Json.Nodes;

namespace Overworld.Cli.Tests;

public sealed class CommandLineTest
{
    public sealed record Paint(string Name, IReadOnlyList<string> Colors, string? BaseColor = null);

    public sealed record Count(int Times);

    public enum Speed
    {
        Slow,
        Fast,
    }

    public sealed record Run(int Laps, bool Warm = false, Speed? Pace = null);

    public sealed record Odd(DateTime When);

    [Fact]
    public void ReadsEachOptionIntoItsCamelCaseKey()
    {
        var json = CommandLine.ReadOptions(typeof(Paint), ["--name", "Sea", "--colors", "#003F5C", "--base-color", "#FFFFFF"]);

        Assert.True(json.IsSuccess);
        Assert.True(JsonNode.DeepEquals(
            JsonNode.Parse("""{"name":"Sea","colors":["#003F5C"],"baseColor":"#FFFFFF"}"""),
            json.Value));
    }

    [Fact]
    public void SplitsAListOnCommasAndTrimsEachItem()
    {
        var json = CommandLine.ReadOptions(typeof(Paint), ["--name", "Sea", "--colors", "#003f5c, #58508d"]);

        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("""{"name":"Sea","colors":["#003f5c","#58508d"]}"""), json.Value));
    }

    [Fact]
    public void LeavesOutAnOptionalOptionThatIsNotGiven()
    {
        var json = CommandLine.ReadOptions(typeof(Paint), ["--name", "Sea", "--colors", "#003F5C"]);

        Assert.False(json.Value.ContainsKey("baseColor"));
    }

    [Theory]
    [InlineData("--name", "--colors", "#003F5C")]
    [InlineData("--colors", "--name", "Sea")]
    public void RefusesToLeaveOutARequiredOption(string missing, params string[] args)
    {
        var json = CommandLine.ReadOptions(typeof(Paint), args);

        Assert.Equal("cli.missing_option", json.Error.Code);
        Assert.Equal($"{missing} is required.", json.Error.Message);
    }

    [Fact]
    public void KnowsWhichOptionsAreRequired()
    {
        Assert.True(CommandLine.IsRequired(typeof(Paint).GetProperty(nameof(Paint.Name))!));
        Assert.False(CommandLine.IsRequired(typeof(Paint).GetProperty(nameof(Paint.BaseColor))!));
    }

    [Theory]
    [InlineData("cli.unknown_option", "--size", "3")]
    [InlineData("cli.unexpected_argument", "Sea")]
    [InlineData("cli.missing_value", "--name")]
    [InlineData("cli.missing_value", "--name", "--colors", "#FFFFFF")]
    [InlineData("cli.repeated_option", "--name", "Sea", "--name", "Fire")]
    public void RefusesArgumentsThatDoNotFit(string code, params string[] args)
    {
        var json = CommandLine.ReadOptions(typeof(Paint), args);

        Assert.False(json.IsSuccess);
        Assert.Equal(code, json.Error.Code);
    }

    [Fact]
    public void NamesOptionsInKebabCase()
    {
        Assert.Equal("--base-color", CommandLine.OptionName(typeof(Paint).GetProperty(nameof(Paint.BaseColor))!));
    }

    [Fact]
    public void ReadsNumbersFlagsAndChoicesAsTheirKinds()
    {
        var options = CommandLine.ReadOptions(typeof(Run), ["--laps", "3", "--warm", "true", "--pace", "fast"]).Value;

        Assert.Equal(3, options["laps"]!.GetValue<int>());
        Assert.True(options["warm"]!.GetValue<bool>());
        Assert.Equal("Fast", options["pace"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("--laps", "three")]
    [InlineData("--warm", "yes")]
    [InlineData("--pace", "sprint")]
    public void RefusesAValueOfTheWrongKind(string option, string value)
    {
        string[] args = option == "--laps" ? [option, value] : ["--laps", "1", option, value];

        Assert.Equal("cli.invalid_value", CommandLine.ReadOptions(typeof(Run), args).Error.Code);
    }

    [Fact]
    public void ShowsWhatEachKindTakes()
    {
        Assert.Equal("<number>", CommandLine.Placeholder(typeof(Run).GetProperty(nameof(Run.Laps))!));
        Assert.Equal("<true|false>", CommandLine.Placeholder(typeof(Run).GetProperty(nameof(Run.Warm))!));
        Assert.Equal("<slow|fast>", CommandLine.Placeholder(typeof(Run).GetProperty(nameof(Run.Pace))!));
    }

    [Fact]
    public void RefusesAnInputItCannotRead()
    {
        var thrown = Assert.Throws<NotSupportedException>(() => CommandLine.ReadOptions(typeof(Odd), []));

        Assert.Contains("Odd.When", thrown.Message);
    }
}
