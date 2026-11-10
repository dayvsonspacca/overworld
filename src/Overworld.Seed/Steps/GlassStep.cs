using System.Security.Cryptography;
using System.Text;
using Overworld.Bedrock.Results;

namespace Overworld.Seed.Steps;

public sealed class GlassStep(string crate, string destination) : IStep
{
    public const string Binary = "overworld-glass";

    private const string Built = "target";

    public async Task<Result<string>> RunAsync(CancellationToken cancellationToken)
    {
        var current = Fingerprint();
        var mark = Path.Combine(destination, PublishStep.Mark);

        if (File.Exists(mark) && File.Exists(Path.Combine(destination, Binary)) && await File.ReadAllTextAsync(mark, cancellationToken) == current)
        {
            return $"kept {Binary} in {destination}, unchanged";
        }

        var built = await Command.CaptureAsync(
            "cargo",
            ["build", "--release", "--manifest-path", Path.Combine(crate, "Cargo.toml")],
            cancellationToken);

        if (!built.IsSuccess)
        {
            return built.Error;
        }

        if (built.Value.ExitCode != 0)
        {
            return new Error("seed.glass_failed", $"cargo build {crate} failed:{Environment.NewLine}{built.Value.Output}");
        }

        Directory.CreateDirectory(destination);
        File.Copy(Path.Combine(crate, Built, "release", Binary), Path.Combine(destination, Binary), overwrite: true);
        await File.WriteAllTextAsync(mark, current, cancellationToken);

        return $"built {Binary} into {destination}";
    }

    private string Fingerprint()
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        foreach (var file in Directory.EnumerateFiles(crate, "*", SearchOption.AllDirectories)
            .Where(file => Path.GetRelativePath(crate, file).Split(Path.DirectorySeparatorChar)[0] != Built)
            .Order(StringComparer.Ordinal))
        {
            hash.AppendData(Encoding.UTF8.GetBytes(Path.GetRelativePath(crate, file)));
            hash.AppendData(File.ReadAllBytes(file));
        }

        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }
}
