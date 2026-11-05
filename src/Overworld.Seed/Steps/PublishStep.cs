using Overworld.Bedrock.Results;

namespace Overworld.Seed.Steps;

public sealed class PublishStep(string project, string destination) : IStep
{
    public async Task<Result<string>> RunAsync(CancellationToken cancellationToken)
    {
        var fresh = destination + ".new";

        if (Directory.Exists(fresh))
        {
            Directory.Delete(fresh, recursive: true);
        }

        if (await PublishAsync(fresh, cancellationToken) is { } error)
        {
            return error;
        }

        if (Directory.Exists(destination))
        {
            Directory.Delete(destination, recursive: true);
        }

        Directory.Move(fresh, destination);

        return $"published {Path.GetFileName(project)} into {destination}";
    }

    private async Task<Error?> PublishAsync(string output, CancellationToken cancellationToken)
    {
        var published = await Command.CaptureAsync(
            "dotnet",
            [
                "publish", project,
                "-c", "Release",
                "--use-current-runtime",
                "--self-contained", "false",
                "-p:PublishSingleFile=true",
                "-o", output,
            ],
            cancellationToken);

        if (!published.IsSuccess)
        {
            return published.Error;
        }

        return published.Value.ExitCode == 0
            ? null
            : new Error("seed.publish_failed", $"dotnet publish {project} failed:{Environment.NewLine}{published.Value.Output}");
    }
}
