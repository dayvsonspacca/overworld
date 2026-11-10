using Overworld.Allay.ClaudeCode;
using Overworld.Allay.Hiring;
using Overworld.Allay.Metering;
using Overworld.Allay.Remote;
using Overworld.Apps.Allay.Desktop;
using Overworld.Apps.Allay.Presentation;
using Overworld.Beacon;
using Overworld.Bedrock.Apps;
using Overworld.Bedrock.Operations;
using Overworld.Chest;
using Overworld.Hosting;
using Overworld.Redstone;
using Overworld.Web;

var app = new AppIdentity("allay");
var data = DataDirectory.For(app.Name);
var crew = new Crew([new ClaudeCode(Path.Combine(data, "claude-code", "skills"))], new StaffFile(data));
var beacon = new BeaconClient(new RedstoneClient(SocketPath.Resolve()));

if (args is ["serve"])
{
    return await AllayService.RunAsync(crew, new Transcripts(Path.Combine(data, "chats")));
}

var list = Operation.From(new ListAgentsOp(crew));
var check = Operation.From(new CheckAgentOp(crew));
var hire = Operation.From(new HireAgentOp(crew, beacon));
var configure = Operation.From(new ConfigureAgentOp(crew, beacon));
var measure = Operation.From(new MeasureAgentOp(crew));

return await AppHost.RunAsync(
    app.Command,
    [list, check, hire, configure, measure],
    args,
    withoutArgs: () => Task.FromResult(WebHost.Run(app, page =>
    {
        var agents = new AgentsViewModel(list, check, hire, configure, measure, TimeProvider.System);
        _ = agents.LoadAsync(CancellationToken.None);
        page.Show(agents);
    })));
