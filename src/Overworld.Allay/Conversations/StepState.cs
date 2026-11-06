using System.Text.Json.Serialization;

namespace Overworld.Allay.Conversations;

[JsonConverter(typeof(JsonStringEnumConverter<StepState>))]
public enum StepState
{
    Pending,
    Doing,
    Done,
}
