using System.Diagnostics;

namespace Overworld.Apps.Crafter.Tests.Library;

public sealed class Remote
{
    private readonly string _seed;

    public Remote(string root)
    {
        _seed = Directory.CreateDirectory(Path.Combine(root, "seed")).FullName;
        Repository = Path.Combine(root, "remote.git");
        Git(_seed, "init", "--quiet");
    }

    public string Repository { get; }

    public static void Git(string folder, params string[] arguments)
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = folder, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in (string[])["-c", "user.name=Test", "-c", "user.email=test@example.com", .. arguments])
        {
            start.ArgumentList.Add(argument);
        }

        using var git = Process.Start(start)!;
        git.WaitForExit();

        if (git.ExitCode != 0)
        {
            throw new InvalidOperationException(git.StandardError.ReadToEnd());
        }
    }

    public Remote Skill(string id, string? json, string? instructions = "Do it well.")
    {
        var place = Directory.CreateDirectory(Path.Combine(_seed, "skills", id)).FullName;

        if (json is not null)
        {
            File.WriteAllText(Path.Combine(place, "skill.json"), json);
        }

        if (instructions is not null)
        {
            File.WriteAllText(Path.Combine(place, "instructions.md"), instructions);
        }

        return this;
    }

    public Remote Publish()
    {
        File.WriteAllText(Path.Combine(_seed, "README.md"), Guid.NewGuid().ToString());
        Git(_seed, "add", "--all");
        Git(_seed, "commit", "--quiet", "-m", "Seed");

        if (Directory.Exists(Repository))
        {
            Git(_seed, "push", "--quiet", Repository, "HEAD");
        }
        else
        {
            Git(_seed, "clone", "--quiet", "--bare", _seed, Repository);
        }

        return this;
    }
}
