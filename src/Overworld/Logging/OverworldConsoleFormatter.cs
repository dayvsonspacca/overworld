using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Overworld.Logging;

internal sealed class OverworldConsoleFormatter() : ConsoleFormatter(FormatterName)
{
    public const string FormatterName = "overworld";

    private const int CategoryWidth = 20;
    private const string Reset = "\e[0m";
    private const string Dim = "\e[2m";

    private static readonly bool UseColors =
        !Console.IsErrorRedirected && Environment.GetEnvironmentVariable("NO_COLOR") is null;

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
        var (label, color) = Level(logEntry.LogLevel);

        Paint(textWriter, DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture), Dim);
        textWriter.Write(' ');
        Paint(textWriter, label, color);
        textWriter.Write(' ');
        Paint(textWriter, ShortCategory(logEntry.Category).PadRight(CategoryWidth), Dim);
        textWriter.Write(' ');
        textWriter.WriteLine(message);

        if (logEntry.Exception is { } exception)
            Paint(textWriter, exception + Environment.NewLine, Dim);
    }

    private static (string Label, string Color) Level(LogLevel level) => level switch
    {
        LogLevel.Trace => ("TRC", "\e[90m"),
        LogLevel.Debug => ("DBG", "\e[90m"),
        LogLevel.Information => ("INF", "\e[32m"),
        LogLevel.Warning => ("WRN", "\e[33m"),
        LogLevel.Error => ("ERR", "\e[31m"),
        LogLevel.Critical => ("CRT", "\e[1;31m"),
        _ => ("???", Reset),
    };

    private static string ShortCategory(string category) =>
        category[(category.LastIndexOf('.') + 1)..];

    private static void Paint(TextWriter writer, string text, string color)
    {
        if (!UseColors)
        {
            writer.Write(text);
            return;
        }

        writer.Write(color);
        writer.Write(text);
        writer.Write(Reset);
    }
}
