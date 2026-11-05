using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using Overworld.Bedrock.Json;
using Overworld.Bedrock.Operations;
using Overworld.Bedrock.Results;

namespace Overworld.Bedrock.Cli;

public sealed class CliHost
{
    private const int Succeeded = 0;
    private const int Failed = 1;
    private const int Misused = 2;

    private readonly string _command;
    private readonly IReadOnlyDictionary<string, Operation> _operations;

    public CliHost(string command, IEnumerable<Operation> operations)
    {
        var list = operations.ToList();

        if (list.GroupBy(operation => operation.Name).FirstOrDefault(group => group.Count() > 1) is { } repeated)
        {
            throw new ArgumentException($"Two operations are named '{repeated.Key}'.", nameof(operations));
        }

        _command = command;
        _operations = list.ToDictionary(operation => operation.Name);
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

        if (!_operations.TryGetValue(args[0], out var operation))
        {
            WriteError(errors, new Error(
                "cli.unknown_operation",
                $"There is no operation '{args[0]}'. Run {_command} --help to see them."));

            return Misused;
        }

        var options = args.Skip(1).ToList();

        if (options.Contains("--help"))
        {
            WriteUsage(output, operation);

            return Succeeded;
        }

        var json = CommandLine.ReadOptions(operation.InputType, options);

        if (!json.IsSuccess)
        {
            WriteError(errors, json.Error);
            errors.WriteLine($"Run {_command} {operation.Name} --help to see its options.");

            return Misused;
        }

        var input = BedrockJson.ReadInput(json.Value, operation.InputType);

        if (!input.IsSuccess)
        {
            WriteError(errors, input.Error);

            return Misused;
        }

        var result = await operation.ExecuteAsync(input.Value, cancellationToken);

        if (!result.IsSuccess)
        {
            WriteError(errors, result.Error);

            return Failed;
        }

        output.WriteLine(JsonSerializer.Serialize(result.Value, BedrockJson.Options));

        return Succeeded;
    }

    private void WriteOverview(TextWriter output)
    {
        output.WriteLine($"Usage: {_command} <operation> [options]");
        output.WriteLine();
        output.WriteLine("Operations:");

        WriteColumns(output, _operations.Values.Select(operation => (operation.Name, operation.Description)));

        output.WriteLine();
        output.WriteLine($"Run {_command} <operation> --help to see its options.");
    }

    private void WriteUsage(TextWriter output, Operation operation)
    {
        var properties = operation.InputType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        output.WriteLine($"Usage: {_command} {operation.Name}{(properties.Length == 0 ? "" : " [options]")}");
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
