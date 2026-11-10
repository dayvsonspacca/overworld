using Overworld.Allay.Conversations;
using Overworld.Apps.Crafter.Presentation.Conversations;
using Overworld.Allay.Presentation.Conversations;

namespace Overworld.Apps.Crafter.Presentation.Tests.Conversations;

public sealed class ViewerViewModelTest
{
    private static readonly PictureView First = new(new Picture("image/png", [1]), "Pasted by you");

    private static readonly PictureView Second = new(new Picture("image/png", [2]), "mockup.png");

    private static readonly PictureView Third = new(new Picture("image/png", [3]), "icon.png");

    private readonly ViewerViewModel _viewer = new(picture => $"/pictures/{picture.Data[0]}.png");

    [Fact]
    public void OpensOnThePictureClickedAndGoesRoundThemAll()
    {
        _viewer.Open([First, Second, Third], Second);

        Assert.True(_viewer.IsOpen);
        Assert.Equal((Second, "2 / 3", true), (_viewer.Current, _viewer.Position, _viewer.HasOthers));

        _viewer.NextCommand.Execute(null);
        Assert.Equal(Third, _viewer.Current);

        _viewer.NextCommand.Execute(null);
        Assert.Equal(First, _viewer.Current);

        _viewer.PreviousCommand.Execute(null);
        Assert.Equal((Third, "3 / 3"), (_viewer.Current, _viewer.Position));
        Assert.Equal("/pictures/3.png", _viewer.FileOfCurrent());

        _viewer.CloseCommand.Execute(null);
        Assert.False(_viewer.IsOpen);
    }

    [Fact]
    public void ShowsAPictureAloneWhenItIsNotAmongTheOthers()
    {
        _viewer.Open([First], Third);

        Assert.Equal((Third, "1 / 1", false), (_viewer.Current, _viewer.Position, _viewer.HasOthers));
    }
}
