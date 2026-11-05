using System.ComponentModel;
using System.Diagnostics;
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
        var info = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var argument in (string[])[
            "publish", project,
            "-c", "Release",
            "--use-current-runtime",
            "--self-contained", "false",
            "-p:PublishSingleFile=true",
            "-o", output])
        {
            info.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = info };

        try
        {
            process.Start();
        }
        catch (Win32Exception)
        {
            return new Error("seed.dotnet_missing", "Could not run dotnet; is the .NET SDK installed?");
        }

        var log = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errors = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        return process.ExitCode == 0
            ? null
            : new Error("seed.publish_failed", $"dotnet publish {project} failed:{Environment.NewLine}{await log}{await errors}");
    }
}
