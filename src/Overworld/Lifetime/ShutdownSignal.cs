using System.Runtime.InteropServices;

namespace Overworld.Lifetime;

internal sealed class ShutdownSignal : IDisposable
{
    private readonly CancellationTokenSource _stop = new();
    private readonly PosixSignalRegistration _onTerm;
    private readonly PosixSignalRegistration _onInt;

    public ShutdownSignal()
    {
        _onTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, RequestStop);
        _onInt = PosixSignalRegistration.Create(PosixSignal.SIGINT, RequestStop);
    }

    public CancellationToken Token => _stop.Token;

    private void RequestStop(PosixSignalContext context)
    {
        context.Cancel = true;
        _stop.Cancel();
    }

    public void Dispose()
    {
        _onTerm.Dispose();
        _onInt.Dispose();
        _stop.Dispose();
    }
}
