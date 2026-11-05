using Overworld.Bedrock.Results;

namespace Overworld.Seed;

public sealed class Repository
{
    private const string Marker = "Overworld.slnx";

    private readonly string _root;

    private Repository(string root) => _root = root;

    public string PathTo(params string[] parts) => Path.Combine([_root, .. parts]);

    public static Result<Repository> Find(string start)
    {
        for (var folder = new DirectoryInfo(start); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, Marker)))
            {
                return new Repository(folder.FullName);
            }
        }

        return new Error(
            "seed.no_repository",
            $"No {Marker} in {start} or above it; run seed from inside the Overworld repository.");
    }
}
