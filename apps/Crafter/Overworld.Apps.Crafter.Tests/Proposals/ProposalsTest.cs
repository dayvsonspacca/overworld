using Overworld.Apps.Crafter.Axioms;
using Overworld.Apps.Crafter.Projects;
using Overworld.Apps.Crafter.Proposals;
using Overworld.Apps.Crafter.Sessions;
using Overworld.Apps.Crafter.Summaries;
using Overworld.Apps.Crafter.Todos;
using Overworld.Mcp;

namespace Overworld.Apps.Crafter.Tests.Proposals;

public sealed class ProposalsTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();

    private readonly Shelf _shelf;

    private readonly AxiomBook _axioms;

    private readonly SummaryStore _summaries = new();

    private readonly string _project;

    private readonly List<Proposal> _asked = [];

    private int _kept;

    public ProposalsTest()
    {
        _shelf = new Shelf(_folder.PathTo("data"));
        _axioms = new AxiomBook(_shelf, new SettingsStore());
        _project = Directory.CreateDirectory(_folder.PathTo("project")).FullName;
        _shelf.AddAsync(_project, CancellationToken.None).GetAwaiter().GetResult();
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _folder.Dispose();

    private Asker Answering(bool approved) => (proposal, _) =>
    {
        _asked.Add(proposal);

        return Task.FromResult(approved);
    };

    [Fact]
    public async Task KeepsAnApprovedAxiom()
    {
        var said = await new ProposeAxiomOp(_axioms, Answering(true), () => _kept++).ExecuteAsync(new ProposeAxiom(_project, " Never add comments "), Token);

        Assert.Equal("The user approved the axiom; the project holds it now.", said.Value);
        Assert.Equal(new AxiomProposal("Never add comments"), Assert.Single(_asked));
        Assert.Equal(["Never add comments"], (await _axioms.ListAsync(_project, Token)).Value);
        Assert.Equal(1, _kept);
    }

    [Fact]
    public async Task LeavesADeclinedAxiomOut()
    {
        var said = await new ProposeAxiomOp(_axioms, Answering(false), () => _kept++).ExecuteAsync(new ProposeAxiom(_project, "Be brief"), Token);

        Assert.Equal("The user declined the axiom.", said.Value);
        Assert.Empty((await _axioms.ListAsync(_project, Token)).Value);
        Assert.Equal(0, _kept);
    }

    [Fact]
    public async Task DoesNotAskForAnAxiomAlreadyHeld()
    {
        await new AddAxiomOp(_axioms).ExecuteAsync(new AddAxiom(_project, "Be brief"), Token);

        var said = await new ProposeAxiomOp(_axioms, Answering(true)).ExecuteAsync(new ProposeAxiom(_project, "Be brief"), Token);

        Assert.Equal("axiom.held", said.Error.Code);
        Assert.Empty(_asked);
    }

    [Fact]
    public async Task ReplacesTheSummaryWhenApprovedShowingTheOneItReplaces()
    {
        await _summaries.WriteAsync(_project, "Old.", Token);

        await new ProposeSummaryOp(_summaries, Answering(true), () => _kept++).ExecuteAsync(new ProposeSummary(_project, "# New\n\nBetter."), Token);

        Assert.Equal(new SummaryProposal("Old.", "# New\n\nBetter."), Assert.Single(_asked));
        Assert.Equal("# New\n\nBetter.", (await _summaries.ReadAsync(_project, Token)).Value.Text);
        Assert.Equal(1, _kept);
    }

    [Fact]
    public async Task KeepsTheSummaryWhenDeclined()
    {
        await _summaries.WriteAsync(_project, "Old.", Token);

        await new ProposeSummaryOp(_summaries, Answering(false)).ExecuteAsync(new ProposeSummary(_project, "New."), Token);

        Assert.Equal("Old.", (await _summaries.ReadAsync(_project, Token)).Value.Text);
    }

    [Fact]
    public void ServesTheProposalsOnlyToAChatThatCanAsk()
    {
        using var host = new McpHttpHost("crafter");
        host.Start();
        var toolbox = new Toolbox(host, new TodoList(_shelf), _axioms, _summaries);

        var plain = toolbox.Admit(_project);
        var asking = toolbox.Admit(_project, Answering(true));

        Assert.DoesNotContain("propose-axiom", plain.FreeTools);
        Assert.Contains("propose-axiom", asking.FreeTools);
        Assert.Contains("propose-summary", asking.FreeTools);
    }
}
