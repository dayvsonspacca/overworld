using Microsoft.Extensions.Logging.Abstractions;
using Overworld.Bedrock.Operations;

namespace Overworld.Redstone.Tests;

public sealed class RoundTripTest : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("redstone-").FullName;
    private readonly Operation _echo = Operation.From(new EchoOp());

    private string Socket => Path.Combine(_folder, "overworld.sock");

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private RedstoneServer Serve() =>
        new(Socket, new Dispatcher([_echo], NullLogger<Dispatcher>.Instance), NullLogger<RedstoneServer>.Instance);

    [Fact]
    public async Task ARemoteOperationRunsInTheServingProcess()
    {
        using var server = Serve();
        server.Start();
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var serving = server.RunAsync(stop.Token);

        var remote = new RemoteOperation(_echo, new RedstoneClient(Socket));
        var answer = await remote.ExecuteAsync(new Shout("lever"), TestContext.Current.CancellationToken);
        var failure = await remote.ExecuteAsync(new Shout(""), TestContext.Current.CancellationToken);

        await stop.CancelAsync();
        await serving;

        Assert.Equal("lever", answer.Value!.ToString());
        Assert.Equal("echo.silence", failure.Error.Code);
    }

    [Fact]
    public async Task TellsWhenNothingIsServing()
    {
        var response = await new RedstoneClient(Socket)
            .SendAsync(new Request("echo"), TestContext.Current.CancellationToken);

        Assert.Equal("daemon.not_running", response.Error!.Code);
    }

    [Fact]
    public void RefusesToStartWhileAnotherServerAnswers()
    {
        using var first = Serve();
        first.Start();

        using var second = Serve();

        Assert.Throws<AlreadyRunningException>(second.Start);
    }

    [Fact]
    public void TakesOverASocketNobodyAnswers()
    {
        File.WriteAllText(Socket, "");

        using var server = Serve();
        server.Start();

        Assert.True(File.Exists(Socket));
    }
}
