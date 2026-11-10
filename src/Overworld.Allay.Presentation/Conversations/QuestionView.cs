using CommunityToolkit.Mvvm.ComponentModel;
using Overworld.Allay.Conversations;

namespace Overworld.Allay.Presentation.Conversations;

public sealed partial class QuestionView : ObservableObject
{
    private readonly Action _changed;

    public QuestionView(Question question, Action changed)
    {
        _changed = changed;
        Question = question;
        Options = [.. question.Options.Select(option => new OptionView(option, this))];
        if (Options.FirstOrDefault(option => option.IsRecommended) is { } recommended)
        {
            recommended.IsPicked = true;
        }
    }

    public Question Question { get; }

    public string Text => Question.Text;

    public string Header => Question.Header;

    public bool Several => Question.Several;

    public IReadOnlyList<OptionView> Options { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAnswered))]
    public partial string Written { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsWriting { get; set; }

    public bool IsAnswered => Options.Any(option => option.IsPicked) || Written.Trim().Length > 0;

    public QuestionAnswer Answer() =>
        new(Question, [.. Options.Where(option => option.IsPicked).Select(option => option.Option)], Written.Trim() is { Length: > 0 } written ? written : null);

    internal void Picked(OptionView option, bool picked)
    {
        if (picked && !Several)
        {
            foreach (var other in Options.Where(other => other != option))
            {
                other.IsPicked = false;
            }

            IsWriting = false;
            Written = string.Empty;
        }

        OnPropertyChanged(nameof(IsAnswered));
        _changed();
    }

    partial void OnIsWritingChanged(bool value)
    {
        if (value && !Several)
        {
            foreach (var option in Options)
            {
                option.IsPicked = false;
            }
        }
    }

    partial void OnWrittenChanged(string value) => _changed();
}
