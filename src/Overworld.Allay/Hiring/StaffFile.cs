using Overworld.Bedrock.Results;
using Overworld.Chest;

namespace Overworld.Allay.Hiring;

public sealed class StaffFile(string directory)
{
    private readonly JsonFile<Staff> _file = new(Path.Combine(directory, "agents.json"), () => new Staff());

    public Task<Result<Staff>> ReadAsync(CancellationToken cancellationToken) => _file.ReadAsync(cancellationToken);

    public Task<Result<Staff>> UpdateAsync(Func<Staff, Result<Staff>> change, CancellationToken cancellationToken) =>
        _file.UpdateAsync(change, cancellationToken);
}
