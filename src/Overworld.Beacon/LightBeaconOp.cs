using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Beacon;

[Operation(Name, "Lights a signal on the beacon, so whoever watches for it reacts, like the shell rereading the theme.")]
public sealed class LightBeaconOp(Beam beam) : IOperation<LightBeacon, string>
{
    internal const string Name = "beacon-light";

    public Task<Result<string>> ExecuteAsync(LightBeacon input, CancellationToken cancellationToken)
    {
        var signal = input.Signal.Trim();

        if (signal.Length == 0)
        {
            return Task.FromResult<Result<string>>(new Error("beacon.signal_empty", "A signal needs a name, like theme."));
        }

        beam.Light(signal);

        return Task.FromResult<Result<string>>(signal);
    }
}
