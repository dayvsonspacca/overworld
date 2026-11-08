using System.Text.Json;

namespace Overworld.Bedrock.Lending;

public interface ILender
{
    ILoan? Lend(JsonElement said);
}
