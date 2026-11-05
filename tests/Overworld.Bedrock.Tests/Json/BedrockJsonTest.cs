using System.Text.Json;
using System.Text.Json.Nodes;
using Overworld.Bedrock.Json;

namespace Overworld.Bedrock.Tests.Json;

public sealed class BedrockJsonTest
{
    public sealed record Paint(string Name, IReadOnlyList<string> Colors, string? BaseColor = null);

    [Fact]
    public void ReadsAnInputFromCamelCaseJson()
    {
        var input = BedrockJson.ReadInput(JsonNode.Parse("""{"name":"Sea","colors":["#003F5C"]}"""), typeof(Paint));

        var paint = Assert.IsType<Paint>(input.Value);
        Assert.Equal("Sea", paint.Name);
        Assert.Equal(["#003F5C"], paint.Colors);
        Assert.Null(paint.BaseColor);
    }

    [Theory]
    [InlineData("""{"colors":["#003F5C"]}""", "'name'")]
    [InlineData("""{"name":null,"colors":[]}""", "'Name'")]
    [InlineData("""{"name":3,"colors":[]}""", "$.name")]
    [InlineData("null", "not null")]
    public void RefusesAnInputThatDoesNotFit(string json, string mentioned)
    {
        var input = BedrockJson.ReadInput(JsonNode.Parse(json), typeof(Paint));

        Assert.False(input.IsSuccess);
        Assert.Equal("input.invalid", input.Error.Code);
        Assert.Contains(mentioned, input.Error.Message);
    }

    [Fact]
    public void WritesIndentedCamelCaseJson()
    {
        var json = JsonSerializer.Serialize(new Paint("Sea", ["#003F5C"]), BedrockJson.Options);

        Assert.Contains("\n", json);
        Assert.Contains("\"baseColor\": null", json);
    }

    [Fact]
    public void CannotBeChangedByAnApp()
    {
        Assert.True(BedrockJson.Options.IsReadOnly);
    }
}
