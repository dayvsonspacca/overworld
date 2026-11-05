using System.Text.Json;
using Overworld.Bedrock.Json;
using Overworld.Bedrock.Results;

namespace Overworld.Seed.Steps;

public sealed class PackagesStep(string list) : IStep
{
    private const int SomeMissing = 127;

    private static readonly string[] Helpers = ["yay", "paru"];

    private sealed record Packages(IReadOnlyList<string> Repo, IReadOnlyList<string> Aur);

    public async Task<Result<string>> RunAsync(CancellationToken cancellationToken)
    {
        var packages = JsonSerializer.Deserialize<Packages>(await File.ReadAllTextAsync(list, cancellationToken), BedrockJson.Options)
            ?? throw new InvalidDataException($"{list} holds null, not the packages Overworld needs.");

        var missing = await MissingAsync([.. packages.Repo, .. packages.Aur], cancellationToken);

        if (!missing.IsSuccess)
        {
            return missing.Error;
        }

        if (missing.Value.Count == 0)
        {
            return "every package Overworld needs is installed";
        }

        var repo = packages.Repo.Where(missing.Value.Contains).ToList();

        if (repo.Count > 0
            && Failed(await Command.AttachAsync("sudo", ["pacman", "-S", "--needed", "--noconfirm", .. repo], null, cancellationToken), "pacman") is { } error)
        {
            return error;
        }

        var aur = packages.Aur.Where(missing.Value.Contains).ToList();

        if (aur.Count > 0)
        {
            var helper = Helper();

            if (helper is null)
            {
                return new Error("seed.packages_failed", $"{string.Join(", ", aur)} come from the AUR; install yay or paru and run Seed again.");
            }

            if (Failed(await Command.AttachAsync(helper, ["-S", "--needed", "--noconfirm", .. aur], null, cancellationToken), helper) is { } failed)
            {
                return failed;
            }
        }

        return $"installed {string.Join(", ", missing.Value)}";
    }

    private static async Task<Result<IReadOnlyList<string>>> MissingAsync(IReadOnlyList<string> packages, CancellationToken cancellationToken)
    {
        var checkedOnes = await Command.CaptureAsync("pacman", ["-T", .. packages], cancellationToken);

        if (!checkedOnes.IsSuccess)
        {
            return checkedOnes.Error;
        }

        return checkedOnes.Value.ExitCode switch
        {
            0 => Result.Success<IReadOnlyList<string>>([]),
            SomeMissing => checkedOnes.Value.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            var code => new Error("seed.packages_failed", $"pacman -T stopped with exit code {code}: {checkedOnes.Value.Output}"),
        };
    }

    // An AUR helper builds as the user and resolves conflicts with what is already installed, which a bare makepkg does not.
    private static string? Helper() =>
        Helpers.FirstOrDefault(helper =>
            (Environment.GetEnvironmentVariable("PATH") ?? "").Split(':', StringSplitOptions.RemoveEmptyEntries)
                .Any(folder => File.Exists(Path.Combine(folder, helper))));

    private static Error? Failed(Result<int> ran, string what) =>
        !ran.IsSuccess ? ran.Error
        : ran.Value != 0 ? new Error("seed.packages_failed", $"{what} stopped with exit code {ran.Value}; its output is above.")
        : null;
}
