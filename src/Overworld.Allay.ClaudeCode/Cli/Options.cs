using Overworld.Allay.Settings;

namespace Overworld.Allay.ClaudeCode.Cli;

internal sealed record Options(string Executable, Mode Mode, string? Model, string? Effort)
{
    public static Options Of(string executable, Setup setup, Capabilities capabilities) =>
        new(executable, setup.Mode, setup.Model, setup.Effort == Settings.Effort.Max ? capabilities.Highest ?? "max" : null);

    public IReadOnlyList<string> Arguments(string? resume)
    {
        List<string> arguments =
        [
            "-p",
            "--input-format", "stream-json",
            "--output-format", "stream-json",
            "--verbose",
            "--include-partial-messages",
            "--permission-prompt-tool", "stdio",
            "--permission-mode", Mode switch
            {
                Mode.Plan => "plan",
                Mode.Edits => "acceptEdits",
                _ => "auto",
            },
        ];

        if (Model is { } model)
        {
            arguments.AddRange(["--model", model]);
        }

        if (Effort is { } effort)
        {
            arguments.AddRange(["--effort", effort]);
        }

        if (resume is { } session)
        {
            arguments.AddRange(["--resume", session]);
        }

        return arguments;
    }
}
