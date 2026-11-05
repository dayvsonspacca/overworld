using System.Text.Json.Nodes;

namespace Overworld.Cli.Tests;

public sealed class CommandLineTest
{
    public sealed record Paint(string Name, IReadOnlyList<string> Colors, string? BaseColor = null);

    public sealed record Count(int Times);

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
    public void RefusesAnInputItCannotRead()
    {
        var thrown = Assert.Throws<NotSupportedException>(() => CommandLine.ReadOptions(typeof(Count), []));

        Assert.Contains("Count.Times", thrown.Message);
    }
}
