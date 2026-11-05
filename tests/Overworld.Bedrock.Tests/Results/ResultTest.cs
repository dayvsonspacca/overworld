using Overworld.Bedrock.Results;

namespace Overworld.Bedrock.Tests.Results;

public sealed class ResultTest
{
    private static readonly Error NotFound = new("thing.not_found", "No thing called 'x'.");

    [Fact]
    public void HoldsTheValueOfASuccess()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void HoldsTheErrorOfAFailure()
    {
        Result<int> result = NotFound;

        Assert.False(result.IsSuccess);
        Assert.Equal(NotFound, result.Error);
    }

    [Fact]
    public void CountsANullValueAsASuccess()
    {
        var result = Result.Success<string?>(null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public void RefusesToGiveTheValueOfAFailure()
    {
        var result = Result.Failure<int>(NotFound);

        var thrown = Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.Contains("thing.not_found", thrown.Message);
    }

    [Fact]
    public void RefusesToGiveTheErrorOfASuccess()
    {
        var result = Result.Success(42);

        Assert.Throws<InvalidOperationException>(() => result.Error);
    }
}
