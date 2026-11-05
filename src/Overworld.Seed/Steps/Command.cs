using System.ComponentModel;
using System.Diagnostics;
using Overworld.Bedrock.Results;

namespace Overworld.Seed.Steps;

public static class Command
{
    public static async Task<Result<(int ExitCode, string Output)>> CaptureAsync(
        string program,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        using var process = Create(program, arguments, folder: null, capture: true);

        if (!Start(process))
        {
            return Missing(program);
        }

        var output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errors = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        return (process.ExitCode, await output + await errors);
    }

    public static async Task<Result<int>> AttachAsync(
        string program,
        IReadOnlyList<string> arguments,
        string? folder,
        CancellationToken cancellationToken)
    {
        using var process = Create(program, arguments, folder, capture: false);

        if (!Start(process))
        {
            return Missing(program);
        }

        await process.WaitForExitAsync(cancellationToken);

        return process.ExitCode;
    }

    private static Process Create(string program, IReadOnlyList<string> arguments, string? folder, bool capture)
    {
        var info = new ProcessStartInfo(program)
        {
            RedirectStandardOutput = capture,
            RedirectStandardError = capture,
            WorkingDirectory = folder ?? "",
        };

        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        return new Process { StartInfo = info };
    }

    private static bool Start(Process process)
    {
        try
        {
            return process.Start();
        }
        catch (Win32Exception)
        {
            return false;
        }
    }

    private static Error Missing(string program) =>
        new("seed.program_missing", $"Could not run {program}; is it installed?");
}
