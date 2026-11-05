using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;
using Overworld.Seed.Steps;

namespace Overworld.Seed;

[Operation("install", "Installs Overworld on this machine: installs the packages it needs, links its config and theme, publishes the daemon and the apps, and puts them on the PATH.")]
public sealed class InstallOp(Func<Install, IReadOnlyList<IStep>> plan) : IOperation<Install, IReadOnlyList<string>>
{
    public async Task<Result<IReadOnlyList<string>>> ExecuteAsync(Install input, CancellationToken cancellationToken)
    {
        List<string> done = [];

        foreach (var step in plan(input))
        {
            var result = await step.RunAsync(cancellationToken);

            if (!result.IsSuccess)
            {
                return done.Count == 0
                    ? result.Error
                    : result.Error with { Message = $"{result.Error.Message} Done before it: {string.Join("; ", done)}." };
            }

            done.Add(result.Value);
        }

        return done;
    }
}
