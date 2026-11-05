using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Overworld.Daemon.Logging;

internal static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddOverworldConsole(this ILoggingBuilder builder) => builder
        .AddConsole(options =>
        {
            options.FormatterName = OverworldConsoleFormatter.FormatterName;
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        })
        .AddConsoleFormatter<OverworldConsoleFormatter, ConsoleFormatterOptions>();
}
