using System.ComponentModel;
using System.Reflection;
using Overworld.Bedrock.Json;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Cli;

public sealed class CliHost
{
    private const int Succeeded = 0;
    private const int Failed = 1;
    private const int Misused = 2;

    private readonly string _command;
    private readonly Catalog _catalog;

    public CliHost(string command, IEnumerable<Operation> operations)
    {
        _command = command;
        _catalog = new Catalog(operations);
    }

    public async Task<int> RunAsync(
        IReadOnlyList<string> args,
        TextWriter output,
        TextWriter errors,
        CancellationToken cancellationToken)
    {
        if (args is [] or ["--help"])
        {
            WriteOverview(output);

            return Succeeded;
        }

        var words = args.TakeWhile(arg => !arg.StartsWith("--", StringComparison.Ordinal)).ToList();

        if (Find(words) is not ({ } operation, var taken))
        {
            WriteError(errors, new Error(
                "cli.unknown_operation",
                $"There is no operation '{(words is [] ? args[0] : string.Join(' ', words))}'. Run {_command} --help to see them."));

            return Misused;
        }

        var options = args.Skip(taken).ToList();

        if (options.Contains("--help"))
        {
            WriteUsage(output, operation);

            return Succeeded;
        }

        var json = CommandLine.ReadOptions(operation.InputType, options);

        if (!json.IsSuccess)
        {
            WriteError(errors, json.Error);
            errors.WriteLine($"Run {_command} {Spoken(operation.Name)} --help to see its options.");

            return Misused;
        }

        var result = await operation.RunAsync(json.Value, cancellationToken);

        if (!result.IsSuccess)
        {
            WriteError(errors, result.Error);

            return result.Error.Code == BedrockJson.InvalidInput ? Misused : Failed;
        }

        output.WriteLine(result.Value?.ToJsonString(BedrockJson.Options) ?? "null");

        return Succeeded;
    }

    private void WriteOverview(TextWriter output)
    {
        output.WriteLine($"Usage: {_command} <operation> [options]");
        output.WriteLine();
        output.WriteLine("Operations:");

        WriteColumns(output, _catalog.All.Select(operation => (Spoken(operation.Name), operation.Description)));

        output.WriteLine();
        output.WriteLine($"Run {_command} <operation> --help to see its options.");
    }

    private void WriteUsage(TextWriter output, Operation operation)
    {
        var properties = operation.InputType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        output.WriteLine($"Usage: {_command} {Spoken(operation.Name)}{(properties.Length == 0 ? "" : " [options]")}");
        output.WriteLine();
        output.WriteLine(operation.Description);

        if (properties.Length == 0)
        {
            return;
        }

        output.WriteLine();
        output.WriteLine("Options:");

        WriteColumns(output, properties
            .Select(property => (
                $"{CommandLine.OptionName(property)} {CommandLine.Placeholder(property)}",
                (CommandLine.IsRequired(property) ? "Required. " : "")
                    + property.GetCustomAttribute<DescriptionAttribute>()?.Description)));
    }

    private (Operation Operation, int Words)? Find(List<string> words)
    {
        for (var count = words.Count; count > 0; count--)
        {
            if (_catalog.Find(string.Join('-', words.Take(count))) is { } operation)
            {
                return (operation, count);
            }
        }

        return null;
    }

    private static string Spoken(string name) => name.Replace('-', ' ');

    private static void WriteColumns(TextWriter output, IEnumerable<(string Left, string Right)> rows)
    {
        var list = rows.ToList();
        var width = list.Select(row => row.Left.Length).DefaultIfEmpty(0).Max();

        foreach (var (left, right) in list)
        {
            output.WriteLine($"  {left.PadRight(width)}  {right}");
        }
    }

    private static void WriteError(TextWriter errors, Error error) =>
        errors.WriteLine($"{error.Code}: {error.Message}");
}
