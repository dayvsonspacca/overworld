using System.Text.Json.Nodes;
using Overworld.Bedrock.Json;

namespace Overworld.Bell.Tests;

public sealed class BellJsonTest
{
    [Fact]
    public void ReadsARingThatAsksQuestions()
    {
        var json = JsonNode.Parse("""
            {
              "from": "crafter",
              "title": "Claude asks",
              "ask": {
                "type": "questions",
                "asked": [{ "header": "Storage", "text": "Where?", "several": false, "choices": [{ "label": "Chest", "detail": null }] }]
              }
            }
            """);

        var ring = (RingBell)BedrockJson.ReadInput(json, typeof(RingBell)).Value;

        var questions = Assert.IsType<Questions>(ring.Ask);
        Assert.Equal("Chest", questions.Asked[0].Choices[0].Label);
    }

    [Fact]
    public void ReadsAnAnswerFromTheShell()
    {
        var json = JsonNode.Parse("""
            { "id": "a", "reply": { "type": "pressed", "key": "once" } }
            """);

        var answer = (AnswerNotice)BedrockJson.ReadInput(json, typeof(AnswerNotice)).Value;

        Assert.Equal(new Pressed("once"), answer.Reply);
    }
}
