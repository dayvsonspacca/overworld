using Overworld.Apps.Crafter.Projects;
using Overworld.Apps.Crafter.Summaries;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Crafter.Tests.Summaries;

public sealed class SummaryOpsTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();
    private readonly Shelf _shelf;
    private readonly SummaryStore _store = new();
    private readonly string _overworld;

    public SummaryOpsTest()
    {
        _shelf = new Shelf(_folder.PathTo("data"));
        _overworld = Directory.CreateDirectory(_folder.PathTo("overworld")).FullName;
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private string SummaryFile => Path.Combine(_overworld, ".crafter", "summary.md");

    public void Dispose() => _folder.Dispose();

    private async Task OpenAsync() => await _shelf.AddAsync(_overworld, Token);

    private Task<Result<Summary>> ShowAsync() =>
        new ShowSummaryOp(_shelf, _store).ExecuteAsync(new ShowSummary(_overworld), Token);

    private Task<Result<Summary>> WriteAsync(string text) =>
        new WriteSummaryOp(_shelf, _store).ExecuteAsync(new WriteSummary(_overworld, text), Token);

    [Fact]
    public async Task ShowsNoSummaryBeforeOneIsWritten()
    {
        await OpenAsync();

        Assert.Equal(new Summary(null), (await ShowAsync()).Value);
    }

    [Fact]
    public async Task WritesTheSummaryIntoTheProjectsOwnFolder()
    {
        await OpenAsync();

        var written = await WriteAsync("\n# Overworld\n\nThe user's own desktop.\n\n");

        Assert.Equal(new Summary("# Overworld\n\nThe user's own desktop."), written.Value);
        Assert.Equal("# Overworld\n\nThe user's own desktop.\n", await File.ReadAllTextAsync(SummaryFile, Token));
        Assert.Equal(written.Value, (await ShowAsync()).Value);
    }

    [Fact]
    public async Task ReadsWhatCrafterWrote()
    {
        await OpenAsync();
        Directory.CreateDirectory(Path.GetDirectoryName(SummaryFile)!);
        await File.WriteAllTextAsync(SummaryFile, "# Overworld\n", Token);

        Assert.Equal(new Summary("# Overworld"), (await ShowAsync()).Value);
    }

    [Fact]
    public async Task TakesTheSummaryAwayWhenGivenABlankOne()
    {
        await OpenAsync();
        await WriteAsync("# Overworld");

        var written = await WriteAsync("   ");

        Assert.Equal(new Summary(null), written.Value);
        Assert.False(File.Exists(SummaryFile));
    }

    [Fact]
    public async Task LeavesTheFileAloneWhenTheSummaryIsTheSame()
    {
        await OpenAsync();
        await WriteAsync("# Overworld");
        var before = File.GetLastWriteTimeUtc(SummaryFile);
        File.SetLastWriteTimeUtc(SummaryFile, before.AddHours(-1));

        await WriteAsync("  # Overworld  ");

        Assert.Equal(before.AddHours(-1), File.GetLastWriteTimeUtc(SummaryFile));
    }

    [Fact]
    public async Task RefusesAProjectThatIsNotOpen()
    {
        Assert.Equal("project.not_open", (await ShowAsync()).Error.Code);
        Assert.Equal("project.not_open", (await WriteAsync("# Overworld")).Error.Code);
    }
}
