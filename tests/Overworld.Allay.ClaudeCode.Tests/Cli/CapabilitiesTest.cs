using Overworld.Allay.ClaudeCode.Cli;
using Overworld.Allay.Settings;

namespace Overworld.Allay.ClaudeCode.Tests.Cli;

public sealed class CapabilitiesTest
{
    public const string Help = """
          --effort <level>                      Effort level for the current session
                                                (low, medium, high, xhigh, max)
          --model <model>                       Model for the current session. Provide
                                                an alias for the latest model (e.g.
                                                'fable', 'opus', or 'sonnet') or a
                                                model's full name.
        """;

    public const string Introduction =
        """{"type":"control_response","response":{"subtype":"success","request_id":"overworld","response":{"models":[{"value":"default","displayName":"Default (recommended)"},{"value":"opus","displayName":"Opus 5.5"},{"value":"claude-sonnet-4-6","displayName":"Sonnet 4.6"},{"value":"claude-next"},{"displayName":"No value"}]}}}""";

    [Fact]
    public void ReadsTheModelsAndEffortsOutOfTheHelp()
    {
        var capabilities = Capabilities.Parse(Help);

        Assert.Equal([new ModelOption("fable", "Fable"), new ModelOption("opus", "Opus"), new ModelOption("sonnet", "Sonnet")], capabilities.Models);
        Assert.Equal(["low", "medium", "high", "xhigh", "max"], capabilities.Efforts);
        Assert.Equal("max", capabilities.Highest);
    }

    [Fact]
    public void TakesTheModelsClaudeCodeIntroduces()
    {
        var capabilities = Capabilities.Parse(Help).Introduced(Introduction);

        Assert.Equal(
            [new ModelOption("opus", "Opus 5.5"), new ModelOption("claude-sonnet-4-6", "Sonnet 4.6"), new ModelOption("claude-next", "Next")],
            capabilities.Models);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not json")]
    [InlineData("""{"type":"control_response","response":{"subtype":"error"}}""")]
    [InlineData("""{"type":"control_response","response":{"response":{"models":[]}}}""")]
    public void KeepsTheAliasesWhenTheIntroductionSaysNothing(string? answer)
    {
        Assert.Equal(["fable", "opus", "sonnet"], Capabilities.Parse(Help).Introduced(answer).Models.Select(model => model.Id));
    }

    [Fact]
    public void KnowsNothingFromAHelpWithoutThoseOptions()
    {
        var capabilities = Capabilities.Parse("Usage: claude [options]");

        Assert.Empty(capabilities.Models);
        Assert.Empty(capabilities.Efforts);
        Assert.Null(capabilities.Highest);
    }

    [Theory]
    [InlineData("claude-opus-5-5", "Opus 5.5")]
    [InlineData("claude-sonnet-4-6", "Sonnet 4.6")]
    [InlineData("opus", "Opus")]
    public void NamesAModelByItsId(string id, string name)
    {
        Assert.Equal(name, ModelName.Of(id));
    }
}
