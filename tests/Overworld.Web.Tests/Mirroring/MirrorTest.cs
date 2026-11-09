using System.Text.Json.Nodes;
using Overworld.Web.Mirroring;

namespace Overworld.Web.Tests.Mirroring;

public sealed class MirrorTest
{
    private readonly Sample _sample = new();

    private readonly Mirror _mirror = new();

    public MirrorTest()
    {
        _mirror.Ignore<Sample>(nameof(Sample.Secret));
        _mirror.Show(_sample);
    }

    private static JsonObject Defined(JsonArray batch, string type) =>
        batch.OfType<JsonObject>().Single(message => message["t"]!.GetValue<string>() == "obj" && message["type"]!.GetValue<string>() == type);

    private int Id(object value) => Defined(_mirror.Whole(), value.GetType().Name)["id"]!.GetValue<int>();

    [Fact]
    public void ShowsEveryObjectReachableFromTheRootWithItsCommands()
    {
        var whole = _mirror.Whole();
        var sample = Defined(whole, nameof(Sample));

        Assert.Equal("first", sample["props"]!["name"]!.GetValue<string>());
        Assert.True(sample["props"]!["child"]!["$ref"] is not null);
        Assert.True(sample["cmds"]!["SayCommand"]!.GetValue<bool>());
        Assert.False(sample["cmds"]!["ShoutCommand"]!.GetValue<bool>());
        Assert.Contains(whole, message => message!["t"]!.GetValue<string>() == "list");
        Assert.Equal("root", whole[^1]!["t"]!.GetValue<string>());
    }

    [Fact]
    public void LeavesOutWhatIsIgnoredAndWhatCannotBeRead()
    {
        var props = Defined(_mirror.Whole(), nameof(Sample))["props"]!.AsObject();

        Assert.False(props.ContainsKey("secret"));
        Assert.False(props.ContainsKey("pending"));
    }

    [Fact]
    public void SendsWhatChangedOnlyOnce()
    {
        _mirror.Whole();
        _sample.Name = "second";
        _sample.Name = "third";
        _sample.Child.Count = 2;

        var changes = _mirror.Drain().OfType<JsonObject>().ToList();

        Assert.Single(changes, change => change["prop"]?.GetValue<string>() == "name" && change["value"]!.GetValue<string>() == "third");
        Assert.Single(changes, change => change["prop"]?.GetValue<string>() == "count");
        Assert.Empty(_mirror.Drain());
    }

    [Fact]
    public void TellsWhenACommandCanRun()
    {
        _mirror.Whole();
        _sample.IsLoud = true;

        Assert.Contains(_mirror.Drain().OfType<JsonObject>(), change => change["t"]!.GetValue<string>() == "can" && change["cmd"]!.GetValue<string>() == "ShoutCommand" && change["value"]!.GetValue<bool>());
    }

    [Fact]
    public void ListsACollectionAgainWhenItChanges()
    {
        _mirror.Whole();
        _sample.Swatches.Add(new Swatch("sand", "#C2B280"));

        var listed = Assert.Single(_mirror.Drain().OfType<JsonObject>(), change => change["t"]!.GetValue<string>() == "list");

        Assert.Equal(2, listed["items"]!.AsArray().Count);
    }

    [Fact]
    public void RunsCommandsAndSetsValuesThePageSends()
    {
        var id = Id(_sample);

        _mirror.Call(id, "SayCommand", JsonValue.Create("hi"));
        _mirror.Call(id, "ShoutCommand", null);
        _mirror.Set(id, "name", JsonValue.Create("typed"));

        Assert.Equal(["hi"], _sample.Heard);
        Assert.Equal("typed", _sample.Name);
    }

    [Fact]
    public void HandsBackTheVeryObjectThePageReferencesByItsId()
    {
        var whole = _mirror.Whole();
        var listed = whole.OfType<JsonObject>().First(message => message["t"]!.GetValue<string>() == "list");
        var swatch = listed["items"]![0]!["$id"]!.GetValue<int>();

        _mirror.Set(Id(_sample), "Picked", new JsonObject { ["$ref"] = swatch });

        Assert.Same(_sample.Swatches[0], _sample.Picked);
    }

    [Fact]
    public async Task CallsMethodsAndGivesACancellationTokenItself()
    {
        var id = Id(_sample);

        var sum = await _mirror.InvokeAsync(id, "Add", [2, 3]);
        var echoed = await _mirror.InvokeAsync(id, "EchoAsync", ["hello"]);

        Assert.Equal(5, sum!.GetValue<double>());
        Assert.Equal("hello", echoed!.GetValue<string>());
    }
}
