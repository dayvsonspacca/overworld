using Overworld.Apps.Crafter.Projects;

namespace Overworld.Apps.Crafter.Tests.Projects;

public sealed class ShelfTest : IDisposable
{
    private readonly TemporaryFolder _folder = new();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public void Dispose() => _folder.Dispose();

    [Fact]
    public async Task IsEmptyBeforeAnyProject()
    {
        var shelf = await new Shelf(_folder.Path).ListAsync(Token);

        Assert.Empty(shelf.Value);
    }

    [Fact]
    public async Task KeepsProjectsInTheOrderTheyWereAdded()
    {
        var shelf = new Shelf(_folder.Path);

        await shelf.AddAsync("/work/overworld", Token);
        await shelf.AddAsync("/work/zarb", Token);

        var listed = await new Shelf(_folder.Path).ListAsync(Token);
        Assert.Equal([new Shelved("/work/overworld"), new Shelved("/work/zarb")], listed.Value);
    }

    [Fact]
    public async Task AddsAProjectOnlyOnce()
    {
        var shelf = new Shelf(_folder.Path);

        await shelf.AddAsync("/work/overworld", Token);
        var added = await shelf.AddAsync("/work/overworld", Token);

        Assert.Equal([new Shelved("/work/overworld")], added.Value);
    }

    [Fact]
    public async Task RemovesAProject()
    {
        var shelf = new Shelf(_folder.Path);
        await shelf.AddAsync("/work/overworld", Token);
        await shelf.AddAsync("/work/zarb", Token);

        var left = await shelf.RemoveAsync("/work/overworld", Token);

        Assert.Equal([new Shelved("/work/zarb")], left.Value);
    }

    [Fact]
    public async Task LeavesTheFileAloneWhenRemovingAProjectItDoesNotHold()
    {
        var shelf = new Shelf(_folder.Path);

        var left = await shelf.RemoveAsync("/work/overworld", Token);

        Assert.Empty(left.Value);
        Assert.False(File.Exists(_folder.PathTo("projects.json")));
    }

    [Fact]
    public async Task WritesTheShapeCrafterWrote()
    {
        await new Shelf(_folder.Path).AddAsync("/work/overworld", Token);

        var json = await File.ReadAllTextAsync(_folder.PathTo("projects.json"), Token);

        Assert.Contains("\"folder\": \"/work/overworld\"", json);
    }

    [Fact]
    public async Task ReadsCrafterFilesThatStillCarryWhenTheyWereOpened()
    {
        await File.WriteAllTextAsync(
            _folder.PathTo("projects.json"),
            """[{ "folder": "/work/overworld", "lastOpened": "2026-11-06T10:02:13-03:00" }]""",
            Token);

        var listed = await new Shelf(_folder.Path).ListAsync(Token);

        Assert.Equal([new Shelved("/work/overworld")], listed.Value);
    }

    [Fact]
    public async Task ReportsAFileItCannotRead()
    {
        await File.WriteAllTextAsync(_folder.PathTo("projects.json"), "not json", Token);

        var listed = await new Shelf(_folder.Path).ListAsync(Token);

        Assert.Equal("file.unreadable", listed.Error.Code);
    }

    [Fact]
    public async Task FindsAProjectByAnySpellingOfItsFolder()
    {
        var shelf = new Shelf(_folder.Path);
        await shelf.AddAsync("/work/overworld", Token);

        var found = await shelf.FindAsync("/work/./overworld/", Token);

        Assert.Equal("/work/overworld", found.Value);
    }

    [Theory]
    [InlineData("/work/zarb")]
    [InlineData("")]
    public async Task FindsNoProjectThatIsNotOpen(string folder)
    {
        var shelf = new Shelf(_folder.Path);
        await shelf.AddAsync("/work/overworld", Token);

        var found = await shelf.FindAsync(folder, Token);

        Assert.Equal("project.not_open", found.Error.Code);
    }

    [Fact]
    public async Task MovesAProjectToAnotherPlace()
    {
        var shelf = new Shelf(_folder.Path);
        await shelf.AddAsync("/work/overworld", Token);
        await shelf.AddAsync("/work/zarb", Token);
        await shelf.AddAsync("/work/dye", Token);

        var moved = await shelf.MoveAsync("/work/dye", 0, Token);

        Assert.Equal([new Shelved("/work/dye"), new Shelved("/work/overworld"), new Shelved("/work/zarb")], moved.Value);
        Assert.Equal(moved.Value, (await new Shelf(_folder.Path).ListAsync(Token)).Value);
    }

    [Fact]
    public async Task MovesAProjectPastTheEndToTheLastPlace()
    {
        var shelf = new Shelf(_folder.Path);
        await shelf.AddAsync("/work/overworld", Token);
        await shelf.AddAsync("/work/zarb", Token);

        var moved = await shelf.MoveAsync("/work/overworld", 9, Token);

        Assert.Equal([new Shelved("/work/zarb"), new Shelved("/work/overworld")], moved.Value);
    }
}
