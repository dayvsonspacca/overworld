using Overworld.Allay.ClaudeCode;
using Overworld.Allay.Hiring;
using Overworld.Apps.Allay.Desktop;
using Overworld.Apps.Allay.Presentation;
using Overworld.Beacon;
using Overworld.Bedrock.Apps;
using Overworld.Bedrock.Operations;
using Overworld.Chest;
using Overworld.Desktop;
using Overworld.Hosting;
using Overworld.Redstone;

var app = new AppIdentity("allay");
var data = DataDirectory.For(app.Name);
var crew = new Crew([new ClaudeCode(Path.Combine(data, "claude-code", "skills"))], new StaffFile(data));
var beacon = new BeaconClient(new RedstoneClient(SocketPath.Resolve()));

var list = Operation.From(new ListAgentsOp(crew));
var check = Operation.From(new CheckAgentOp(crew));
var hire = Operation.From(new HireAgentOp(crew, beacon));
var configure = Operation.From(new ConfigureAgentOp(crew, beacon));

return await AppHost.RunAsync(
    app.Command,
    [list, check, hire, configure],
    args,
    withoutArgs: () => Task.FromResult(DesktopHost.Run(app, frame =>
    {
        var agents = new AgentsViewModel(list, check, hire, configure);
        Shortcuts.Register(frame, agents);
        _ = agents.LoadAsync(CancellationToken.None);

        return new AgentsView { DataContext = agents };
    }, args)));
