using Overworld.Desktop.Commands;
using Overworld.Desktop.Windowing;

namespace Overworld.Desktop;

public sealed record Frame(CommandBook Commands, TitleBar Bar);
