using CommunityToolkit.Mvvm.ComponentModel;
using Overworld.Allay.Conversations;

namespace Overworld.Allay.Presentation.Conversations;

public sealed partial class OptionView(Option option, QuestionView question) : ObservableObject
{
    public Option Option { get; } = option;

    public string Label => Option.Label;

    public string Description => Option.Description;

    public bool HasDescription => Description.Length > 0;

    [ObservableProperty]
    public partial bool IsPicked { get; set; }

    partial void OnIsPickedChanged(bool value) => question.Picked(this, value);
}
