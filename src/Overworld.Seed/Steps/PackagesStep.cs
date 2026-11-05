using System.Text.Json;
using Overworld.Bedrock.Json;
using Overworld.Bedrock.Results;

namespace Overworld.Seed.Steps;

public sealed class PackagesStep(string list) : IStep
{
    private const int SomeMissing = 127;

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

        foreach (var package in packages.Aur.Where(missing.Value.Contains))
        {
            if (await BuildAsync(package, cancellationToken) is { } failed)
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

    private static async Task<Error?> BuildAsync(string package, CancellationToken cancellationToken)
    {
        var folder = Directory.CreateTempSubdirectory("seed-aur-").FullName;

        try
        {
            var cloned = await Command.CaptureAsync(
                "git", ["clone", "--depth", "1", $"https://aur.archlinux.org/{package}.git", folder], cancellationToken);

            if (!cloned.IsSuccess)
            {
                return cloned.Error;
            }

            if (cloned.Value.ExitCode != 0)
            {
                return new Error("seed.packages_failed", $"Could not clone {package} from the AUR: {cloned.Value.Output}");
            }

            return Failed(await Command.AttachAsync("makepkg", ["-si", "--noconfirm"], folder, cancellationToken), $"makepkg for {package}");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private static Error? Failed(Result<int> ran, string what) =>
        !ran.IsSuccess ? ran.Error
        : ran.Value != 0 ? new Error("seed.packages_failed", $"{what} stopped with exit code {ran.Value}; its output is above.")
        : null;
}
