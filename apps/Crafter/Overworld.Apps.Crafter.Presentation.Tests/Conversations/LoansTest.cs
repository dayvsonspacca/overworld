using System.Text.Json;
using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Presentation.Conversations;
using Overworld.Apps.Crafter.Presentation.Tests.Fakes;
using Overworld.Bedrock.Lending;

namespace Overworld.Apps.Crafter.Presentation.Tests.Conversations;

public sealed class LoansTest : IDisposable
{
    private readonly Bench _bench = new();

    private Timeline Timeline() => new(lenders: [_bench.Lender]);

    [Theory]
    [InlineData(ActKind.Tool)]
    [InlineData(ActKind.Command)]
    public void ShowsWhatAnAppLentRightUnderTheStepThatBroughtIt(ActKind kind)
    {
        var timeline = Timeline();
        timeline.Add(new Act("1", kind, "overworld:dye show"));
        timeline.Add(new Speech("Here it is."));

        timeline.Add(new ActResult("1", true, """{ "lent": "dye:palette:Sea", "title": "Sea" }"""));

        var shown = Assert.IsType<LoanEntry>(timeline.Entries[1]);
        Assert.Equal(("dye:palette:Sea", "Sea"), (shown.Loan.Key, shown.Loan.Title));
        Assert.IsType<SpeechEntry>(timeline.Entries[2]);
    }

    [Fact]
    public void ShowsALoanOnceThoughItComesBack()
    {
        var timeline = Timeline();
        timeline.Add(new Act("1", ActKind.Tool, "crafter: paint"));
        timeline.Add(new ActResult("1", true, """{ "lent": "painting:1" }"""));
        timeline.Add(new Prompt("Again", [], []));
        timeline.Add(new Act("2", ActKind.Tool, "crafter: paint"));

        timeline.Add(new ActResult("2", true, """{ "lent": "painting:1" }"""));

        Assert.Single(timeline.Entries.OfType<LoanEntry>());
    }

    [Fact]
    public void ShowsEachLoanOfItsOwn()
    {
        var timeline = Timeline();
        timeline.Add(new Act("1", ActKind.Tool, "crafter: dye-show"));
        timeline.Add(new ActResult("1", true, """{ "lent": "dye:palette:Sea" }"""));
        timeline.Add(new Act("2", ActKind.Tool, "crafter: dye-show"));

        timeline.Add(new ActResult("2", true, """{ "lent": "dye:palette:Forest" }"""));

        Assert.Equal(2, timeline.Entries.OfType<LoanEntry>().Count());
    }

    [Theory]
    [InlineData(false, """{ "lent": "dye:palette:Sea" }""")]
    [InlineData(true, """{ "id": 3, "title": "Buy milk" }""")]
    [InlineData(true, "[1, 2]")]
    [InlineData(true, "{ not json")]
    public void ShowsNothingForAnyOtherResult(bool succeeded, string output)
    {
        var timeline = Timeline();
        timeline.Add(new Act("1", ActKind.Tool, "something"));

        timeline.Add(new ActResult("1", succeeded, output));

        Assert.IsType<ActEntry>(Assert.Single(timeline.Entries));
    }

    [Fact]
    public void ShowsNothingWithoutLenders()
    {
        var timeline = new Timeline();
        timeline.Add(new Act("1", ActKind.Tool, "crafter: paint"));

        timeline.Add(new ActResult("1", true, """{ "lent": "painting:1" }"""));

        Assert.IsType<ActEntry>(Assert.Single(timeline.Entries));
    }

    [Fact]
    public void AsksTheLendersInTurnUntilOneLends()
    {
        var timeline = new Timeline(lenders: [new Refusing(), _bench.Lender]);
        timeline.Add(new Act("1", ActKind.Tool, "crafter: dye-show"));

        timeline.Add(new ActResult("1", true, """{ "lent": "dye:palette:Sea" }"""));

        Assert.IsType<LoanEntry>(timeline.Entries[1]);
    }

    [Fact]
    public async Task SpreadsALoanOverTheConversationUntilClosed()
    {
        var folder = await _bench.ProjectAsync("overworld");
        var conversation = new ConversationViewModel(folder, Bench.Chat("Palettes"), _bench.Abilities);
        var loan = new LoanEntry(new ScriptedLender.Loan("dye:palette:Sea", "Sea"));

        conversation.ExpandLoanCommand.Execute(loan);

        Assert.Same(loan, conversation.Spread);

        conversation.CloseSpreadCommand.Execute(null);

        Assert.Null(conversation.Spread);
    }

    public void Dispose() => _bench.Dispose();

    private sealed class Refusing : ILender
    {
        public ILoan? Lend(JsonElement said) => null;
    }
}
