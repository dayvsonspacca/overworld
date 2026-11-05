using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Overworld.Apps.Dye.Palettes;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Apps.Dye.Presentation;

public sealed partial class NewPaletteViewModel(Operation<SavePalette, Palette> save) : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = "";

    [ObservableProperty]
    public partial string Colors { get; set; } = "";

    [ObservableProperty]
    public partial string? NameError { get; set; }

    [ObservableProperty]
    public partial string? ColorsError { get; set; }

    [ObservableProperty]
    public partial string? Problem { get; set; }

    [ObservableProperty]
    public partial bool IsSaving { get; set; }

    public event EventHandler<Palette>? Saved;

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        Forget();
        IsSaving = true;

        var colors = Colors.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var saved = await save.ExecuteAsync(new SavePalette(Name, colors), cancellationToken);

        IsSaving = false;

        if (!saved.IsSuccess)
        {
            Show(saved.Error);

            return;
        }

        Clear();
        Saved?.Invoke(this, saved.Value);
    }

    [RelayCommand]
    private void Clear()
    {
        Name = "";
        Colors = "";
        Forget();
    }

    private void Forget()
    {
        NameError = null;
        ColorsError = null;
        Problem = null;
    }

    private void Show(Error error)
    {
        switch (error.Code)
        {
            case "palette.name_empty" or "palette.exists":
                NameError = error.Message;
                break;
            case "palette.colors_empty" or "color.invalid":
                ColorsError = error.Message;
                break;
            default:
                Problem = error.Message;
                break;
        }
    }
}
